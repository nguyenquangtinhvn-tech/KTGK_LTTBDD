import 'package:flutter/material.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

import 'models/student_profile.dart';
import 'repositories/student_repository.dart';
import 'screens/login_screen.dart';
import 'screens/student_shell.dart';
import 'services/api_client.dart';

void main() {
  WidgetsFlutterBinding.ensureInitialized();
  runApp(const ToeicStudentApp());
}

class ToeicStudentApp extends StatefulWidget {
  const ToeicStudentApp({super.key});

  @override
  State<ToeicStudentApp> createState() => _ToeicStudentAppState();
}

class _ToeicStudentAppState extends State<ToeicStudentApp> {
  late final ApiClient _apiClient;
  late final StudentRepository _repository;
  StudentProfile? _profile;
  Object? _startupError;
  bool _checkingSession = true;

  @override
  void initState() {
    super.initState();
    _apiClient = ApiClient(
      secureStorage: const FlutterSecureStorage(),
      onUnauthorized: _handleUnauthorized,
    );
    _repository = StudentRepository(_apiClient);
    _restoreSession();
  }

  Future<void> _restoreSession() async {
    setState(() {
      _checkingSession = true;
      _startupError = null;
    });

    try {
      final profile = await _repository.restoreStudentSession();
      if (!mounted) return;
      setState(() {
        _profile = profile;
        _checkingSession = false;
      });
    } catch (error) {
      if (!mounted) return;
      setState(() {
        _startupError = error;
        _checkingSession = false;
      });
    }
  }

  void _handleUnauthorized() {
    if (!mounted || _profile == null) return;
    setState(() {
      _profile = null;
      _startupError = ApiException.sessionExpired();
    });
  }

  @override
  void dispose() {
    _apiClient.dispose();
    super.dispose();
  }

  Future<void> _signIn(String username, String password) async {
    final profile = await _repository.signIn(username, password);
    if (!mounted) return;
    setState(() {
      _profile = profile;
      _startupError = null;
    });
  }

  Future<void> _signOut() async {
    await _repository.signOut();
    if (!mounted) return;
    setState(() {
      _profile = null;
      _startupError = null;
    });
  }

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'TOEIC Reading Student',
      debugShowCheckedModeBanner: false,
      theme: ThemeData(
        useMaterial3: true,
        colorScheme: ColorScheme.fromSeed(
          seedColor: const Color(0xFF2864E8),
          surface: const Color(0xFFF6F8FC),
        ),
        scaffoldBackgroundColor: const Color(0xFFF6F8FC),
        appBarTheme: const AppBarTheme(
          backgroundColor: Color(0xFFF6F8FC),
          foregroundColor: Color(0xFF17233B),
          elevation: 0,
          centerTitle: false,
        ),
        cardTheme: CardThemeData(
          color: Colors.white,
          elevation: 0,
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(20),
            side: const BorderSide(color: Color(0xFFE8EDF5)),
          ),
        ),
        inputDecorationTheme: InputDecorationTheme(
          filled: true,
          fillColor: Colors.white,
          border: OutlineInputBorder(
            borderRadius: BorderRadius.circular(14),
            borderSide: const BorderSide(color: Color(0xFFE1E7F0)),
          ),
          enabledBorder: OutlineInputBorder(
            borderRadius: BorderRadius.circular(14),
            borderSide: const BorderSide(color: Color(0xFFE1E7F0)),
          ),
          focusedBorder: OutlineInputBorder(
            borderRadius: BorderRadius.circular(14),
            borderSide: const BorderSide(color: Color(0xFF2864E8), width: 1.5),
          ),
          contentPadding: const EdgeInsets.symmetric(
            horizontal: 16,
            vertical: 16,
          ),
        ),
      ),
      home: _buildHome(),
    );
  }

  Widget _buildHome() {
    if (_checkingSession) {
      return const Scaffold(body: Center(child: CircularProgressIndicator()));
    }

    if (_profile != null) {
      return StudentShell(
        profile: _profile!,
        repository: _repository,
        onSignOut: _signOut,
      );
    }

    if (_startupError != null) {
      return _SessionUnavailableScreen(
        message: errorMessage(_startupError!),
        onRetry: _restoreSession,
        onSignOut: _signOut,
      );
    }

    return LoginScreen(onSignIn: _signIn);
  }
}

class _SessionUnavailableScreen extends StatelessWidget {
  const _SessionUnavailableScreen({
    required this.message,
    required this.onRetry,
    required this.onSignOut,
  });

  final String message;
  final Future<void> Function() onRetry;
  final Future<void> Function() onSignOut;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: SafeArea(
        child: Center(
          child: ConstrainedBox(
            constraints: const BoxConstraints(maxWidth: 420),
            child: Padding(
              padding: const EdgeInsets.all(24),
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  const Icon(
                    Icons.cloud_off_rounded,
                    size: 48,
                    color: Color(0xFF2864E8),
                  ),
                  const SizedBox(height: 16),
                  Text(
                    'Không thể khôi phục phiên đăng nhập',
                    textAlign: TextAlign.center,
                    style: Theme.of(context).textTheme.titleLarge
                        ?.copyWith(fontWeight: FontWeight.w700),
                  ),
                  const SizedBox(height: 8),
                  Text(message, textAlign: TextAlign.center),
                  const SizedBox(height: 24),
                  SizedBox(
                    width: double.infinity,
                    child: FilledButton(
                      onPressed: onRetry,
                      child: const Text('Thử kết nối lại'),
                    ),
                  ),
                  TextButton(
                    onPressed: onSignOut,
                    child: const Text('Xóa phiên và đăng nhập lại'),
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}
