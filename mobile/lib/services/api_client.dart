import 'dart:async';
import 'dart:convert';
import 'dart:io';

import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:http/http.dart' as http;

const _tokenKey = 'toeic.student.accessToken';
const _tokenExpiryKey = 'toeic.student.accessTokenExpiresAt';
const _apiBaseUrl = String.fromEnvironment(
  'API_BASE_URL',
  defaultValue: 'https://localhost:44311',
);

class ApiException implements Exception {
  const ApiException({
    this.statusCode,
    required this.message,
    this.isNetworkError = false,
  });

  factory ApiException.sessionExpired() => const ApiException(
    statusCode: 401,
    message: 'Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.',
  );

  final int? statusCode;
  final String message;
  final bool isNetworkError;

  String get userMessage {
    if (isNetworkError) {
      return 'Không thể kết nối máy chủ. Hãy kiểm tra địa chỉ API và mạng '
          'rồi thử lại. Chi tiết: $message';
    }
    final code = statusCode;
    if (code != null && code >= 500) {
      return 'Máy chủ đang gặp sự cố. $message';
    }
    return switch (code) {
      400 => 'Dữ liệu gửi lên không hợp lệ. $message',
      401 => 'Thông tin đăng nhập không hợp lệ hoặc phiên đã hết hạn. $message',
      403 => 'Tài khoản không có quyền thực hiện thao tác này. $message',
      404 => 'Không tìm thấy dữ liệu được yêu cầu. $message',
      _ => message,
    };
  }

  @override
  String toString() => userMessage;
}

String errorMessage(Object error) {
  if (error is ApiException) return error.userMessage;
  if (error is FormatException) return error.message;
  return 'Đã xảy ra lỗi không mong muốn: $error';
}

class ApiClient {
  ApiClient({
    required this.secureStorage,
    http.Client? httpClient,
    String baseUrl = _apiBaseUrl,
    this.onUnauthorized,
  }) : _httpClient = httpClient ?? http.Client(),
       _baseUri = Uri.parse(baseUrl.endsWith('/') ? baseUrl : '$baseUrl/');

  final FlutterSecureStorage secureStorage;
  final http.Client _httpClient;
  final Uri _baseUri;
  final void Function()? onUnauthorized;

  Future<String?> readToken() async {
    final token = await secureStorage.read(key: _tokenKey);
    if (token == null || token.isEmpty) return null;

    final expiryValue = await secureStorage.read(key: _tokenExpiryKey);
    final expiresAt = int.tryParse(expiryValue ?? '');
    if (expiresAt != null &&
        expiresAt <= DateTime.now().millisecondsSinceEpoch) {
      await clearToken();
      return null;
    }
    return token;
  }

  Future<void> saveToken(String token, {int? expiresInSeconds}) async {
    await secureStorage.write(key: _tokenKey, value: token);
    if (expiresInSeconds != null && expiresInSeconds > 0) {
      final expiresAt = DateTime.now()
          .add(Duration(seconds: expiresInSeconds))
          .millisecondsSinceEpoch;
      await secureStorage.write(key: _tokenExpiryKey, value: '$expiresAt');
    } else {
      await secureStorage.delete(key: _tokenExpiryKey);
    }
  }

  Future<void> clearToken() async {
    await secureStorage.delete(key: _tokenKey);
    await secureStorage.delete(key: _tokenExpiryKey);
  }

  Future<dynamic> get(String path, {Map<String, String>? queryParameters}) {
    final uri = _baseUri
        .resolve(path)
        .replace(queryParameters: queryParameters);
    return _request(uri, method: 'GET');
  }

  Future<dynamic> post(
    String path,
    Map<String, Object?> body, {
    Map<String, String>? queryParameters,
  }) {
    final uri = _baseUri
        .resolve(path)
        .replace(queryParameters: queryParameters);
    return _request(uri, method: 'POST', body: body);
  }

  Map<String, String> _headers({bool includeJson = false}) {
    final headers = <String, String>{'Accept': 'application/json'};
    if (includeJson) headers['Content-Type'] = 'application/json';
    return headers;
  }

  Future<dynamic> _request(
    Uri uri, {
    required String method,
    Map<String, Object?>? body,
  }) async {
    final token = await readToken();
    final headers = _headers(includeJson: body != null);
    if (token != null && token.isNotEmpty) {
      headers['Authorization'] = 'Bearer $token';
    }

    try {
      final request = method == 'GET'
          ? _httpClient.get(uri, headers: headers)
          : _httpClient.post(uri, headers: headers, body: jsonEncode(body));
      final response = await request.timeout(const Duration(seconds: 25));
      dynamic decoded;
      if (response.body.isNotEmpty) {
        try {
          decoded = jsonDecode(response.body);
        } on FormatException {
          throw const ApiException(
            message: 'Máy chủ trả về dữ liệu không phải JSON hợp lệ.',
          );
        }
      }

      if (response.statusCode == 401) {
        await clearToken();
        onUnauthorized?.call();
      }

      if (response.statusCode < 200 || response.statusCode >= 300) {
        throw ApiException(
          statusCode: response.statusCode,
          message:
              _extractErrorMessage(decoded) ??
              'Yêu cầu thất bại (HTTP ${response.statusCode}).',
        );
      }

      if (decoded is Map<String, dynamic>) {
        final error = decoded['error'];
        if (error is Map<String, dynamic>) {
          throw ApiException(
            message: error['message'] is String
                ? error['message'] as String
                : 'Máy chủ trả về lỗi nghiệp vụ.',
          );
        }
        return decoded.containsKey('result') ? decoded['result'] : decoded;
      }

      return decoded;
    } on ApiException {
      rethrow;
    } on SocketException catch (error) {
      throw ApiException(message: error.message, isNetworkError: true);
    } on HandshakeException catch (error) {
      throw ApiException(
        message: 'Chứng chỉ HTTPS không được tin cậy: ${error.message}',
        isNetworkError: true,
      );
    } on TimeoutException {
      throw const ApiException(
        message: 'Kết nối quá thời gian chờ.',
        isNetworkError: true,
      );
    } on http.ClientException catch (error) {
      throw ApiException(message: error.message, isNetworkError: true);
    }
  }

  String? _extractErrorMessage(Object? decoded) {
    if (decoded is Map<String, dynamic>) {
      final error = decoded['error'];
      if (error is Map<String, dynamic> && error['message'] is String) {
        return error['message'] as String;
      }
      if (decoded['message'] is String) {
        return decoded['message'] as String;
      }
    }
    return null;
  }

  void dispose() => _httpClient.close();
}
