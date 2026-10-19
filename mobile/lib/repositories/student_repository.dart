import '../models/exam.dart';
import '../models/student_attempt.dart';
import '../models/student_profile.dart';
import '../services/api_client.dart';

class StudentRepository {
  const StudentRepository(this._api);

  final ApiClient _api;

  Future<StudentProfile?> restoreStudentSession() async {
    final token = await _api.readToken();
    if (token == null || token.isEmpty) return null;

    final profile = await _getCurrentProfile();
    if (!profile.isStudent) {
      await _api.clearToken();
      return null;
    }
    return profile;
  }

  Future<StudentProfile> signIn(String username, String password) async {
    final result = await _api.post('api/TokenAuth/Authenticate', {
      'userNameOrEmailAddress': username.trim(),
      'password': password,
      'rememberClient': true,
    });
    if (result is! Map<String, dynamic> ||
        result['accessToken'] is! String ||
        (result['accessToken'] as String).isEmpty) {
      throw const ApiException(
        message: 'Backend không trả về access token hợp lệ.',
      );
    }

    final expiresInSeconds = result['expireInSeconds'];
    await _api.saveToken(
      result['accessToken'] as String,
      expiresInSeconds: expiresInSeconds is num
          ? expiresInSeconds.toInt()
          : null,
    );
    try {
      final profile = await _getCurrentProfile();
      if (!profile.isStudent) {
        throw const ApiException(
          statusCode: 403,
          message: 'Ứng dụng này chỉ dành cho tài khoản có vai trò Student.',
        );
      }
      return profile;
    } catch (_) {
      await _api.clearToken();
      rethrow;
    }
  }

  Future<StudentProfile> _getCurrentProfile() async {
    final result = await _api.get(
      'api/services/app/Session/GetCurrentLoginInformations',
    );
    if (result is! Map<String, dynamic> || result['user'] is! Map) {
      throw const ApiException(
        statusCode: 401,
        message: 'Backend không trả về hồ sơ phiên đăng nhập.',
      );
    }

    return StudentProfile.fromJson(
      Map<String, dynamic>.from(result['user'] as Map),
    );
  }

  Future<List<Exam>> getPublishedExams() async {
    final result = await _api.get('api/services/app/Exam/GetPublishedExams');
    if (result is! List) {
      throw const ApiException(
        message: 'Backend trả về danh sách đề thi không đúng định dạng.',
      );
    }

    return result
        .map((item) {
          if (item is! Map) {
            throw const ApiException(
              message: 'Một đề thi trong danh sách không đúng định dạng.',
            );
          }
          return Exam.fromJson(Map<String, dynamic>.from(item));
        })
        .toList(growable: false);
  }

  Future<ExamDetail> getExamDetail(int examId) async {
    final result = await _api.get(
      'api/services/app/Exam/GetExamDetail',
      queryParameters: {'id': '$examId'},
    );
    if (result is! Map) {
      throw const ApiException(
        message: 'Backend trả về chi tiết đề thi không đúng định dạng.',
      );
    }
    return ExamDetail.fromJson(Map<String, dynamic>.from(result));
  }

  Future<StudentAttempt> createAttempt(int examId) async {
    final result = await _api.post(
      'api/services/app/StudentExamAttempt/CreateAttempt',
      const {},
      queryParameters: {'examId': '$examId'},
    );
    return _readAttempt(result);
  }

  Future<StudentAttempt> getAttempt(int attemptId) async {
    final result = await _api.get(
      'api/services/app/StudentExamAttempt/GetAttempt',
      queryParameters: {'attemptId': '$attemptId'},
    );
    return _readAttempt(result);
  }

  Future<StudentAttempt> saveAnswer({
    required int attemptId,
    required int questionId,
    required int? selectedOptionId,
  }) async {
    final result = await _api.post(
      'api/services/app/StudentExamAttempt/SaveAnswer',
      {
        'attemptId': attemptId,
        'questionId': questionId,
        'selectedOptionId': selectedOptionId,
      },
    );
    return _readAttempt(result);
  }

  Future<StudentExamResult> submitAttempt(int attemptId) async {
    final result = await _api.post(
      'api/services/app/StudentExamAttempt/SubmitAttempt',
      const {},
      queryParameters: {'attemptId': '$attemptId'},
    );
    if (result is! Map) {
      throw const ApiException(
        message: 'Backend trả về kết quả nộp bài không đúng định dạng.',
      );
    }
    return StudentExamResult.fromJson(Map<String, dynamic>.from(result));
  }

  Future<List<StudentExamHistoryItem>> getMyAttemptHistory() async {
    final result = await _api.get(
      'api/services/app/StudentExamAttempt/GetMyAttemptHistory',
    );
    if (result is! List) {
      throw const ApiException(
        message: 'Backend trả về lịch sử thi không đúng định dạng.',
      );
    }
    return result.map((item) {
      if (item is! Map) {
        throw const ApiException(
          message: 'Một kết quả trong lịch sử không đúng định dạng.',
        );
      }
      return StudentExamHistoryItem.fromJson(Map<String, dynamic>.from(item));
    }).toList(growable: false);
  }

  StudentAttempt _readAttempt(Object? result) {
    if (result is! Map) {
      throw const ApiException(
        message: 'Backend trả về lượt thi không đúng định dạng.',
      );
    }
    return StudentAttempt.fromJson(Map<String, dynamic>.from(result));
  }

  Future<void> signOut() => _api.clearToken();
}
