import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile/models/exam.dart';
import 'package:mobile/models/student_attempt.dart';
import 'package:mobile/models/student_profile.dart';
import 'package:mobile/repositories/student_repository.dart';
import 'package:mobile/screens/login_screen.dart';
import 'package:mobile/screens/student_shell.dart';

void main() {
  test('student profile reads roles returned by the session API', () {
    final profile = StudentProfile.fromJson({
      'id': 12,
      'name': 'Mai',
      'surname': 'Nguyen',
      'userName': 'mai',
      'emailAddress': 'mai@example.test',
      'roleNames': ['Student'],
    });

    expect(profile.displayName, 'Mai Nguyen');
    expect(profile.isStudent, isTrue);
  });

  test('exam detail uses API question and passage content', () {
    final detail = ExamDetail.fromJson({
      'id': 7,
      'title': 'Practice Test',
      'description': 'Published test',
      'durationMinutes': 75,
      'readingPassages': [
        {
          'id': 3,
          'partNumber': 7,
          'title': 'Email',
          'content': 'Please reply by Friday.',
          'imageUrl': null,
          'orderNumber': 1,
        },
      ],
      'questions': [
        {'partNumber': 7},
        {'partNumber': 5},
        {'partNumber': 7},
      ],
    });

    expect(detail.questionCount, 3);
    expect(detail.parts, [5, 7]);
    expect(detail.readingPassages.single.content, 'Please reply by Friday.');
  });

  test('attempt model reads backend answer state without answer keys', () {
    final attempt = StudentAttempt.fromJson({
      'attemptId': 9,
      'examId': 7,
      'examTitle': 'Practice Test',
      'startedAtUtc': '2026-01-01T10:00:00Z',
      'expiresAtUtc': '2026-01-01T11:15:00Z',
      'remainingSeconds': 4500,
      'isSubmitted': false,
      'readingPassages': [],
      'questions': [
        {
          'id': 31,
          'partNumber': 5,
          'questionNumber': 101,
          'questionText': 'Choose an answer.',
          'options': [
            {'id': 81, 'label': 'A', 'content': 'Choice A'},
            {'id': 82, 'label': 'B', 'content': 'Choice B'},
          ],
        },
      ],
      'answers': [
        {'questionId': 31, 'selectedOptionId': 82},
      ],
      'result': null,
    });

    expect(attempt.questions.single.options.last.content, 'Choice B');
    expect(attempt.answers[31], 82);
    expect(attempt.isSubmitted, isFalse);
  });

  testWidgets('login screen provides username and password inputs', (
    tester,
  ) async {
    await tester.pumpWidget(
      MaterialApp(home: LoginScreen(onSignIn: (_, _) async {})),
    );

    expect(find.widgetWithText(FilledButton, 'Đăng nhập'), findsOneWidget);
    expect(find.byType(TextFormField), findsNWidgets(2));
    expect(find.text('Tên đăng nhập hoặc email'), findsOneWidget);
    expect(find.text('Mật khẩu'), findsOneWidget);
  });

  testWidgets('student shell shows server-backed empty history state', (
    tester,
  ) async {
    await tester.pumpWidget(
      MaterialApp(
        home: StudentShell(
          profile: const StudentProfile(
            id: 1,
            name: 'Lan',
            surname: 'Tran',
            username: 'lan',
            emailAddress: 'lan@example.test',
            roleNames: ['Student'],
          ),
          repository: _FakeStudentRepository(),
          onSignOut: () async {},
        ),
      ),
    );
    await tester.pumpAndSettle();

    await tester.tap(find.text('Lịch sử'));
    await tester.pumpAndSettle();

    expect(find.text('Lịch sử thi'), findsOneWidget);
    expect(
      find.text('Chưa có lượt thi hoàn thành'),
      findsOneWidget,
    );
  });
}

class _FakeStudentRepository implements StudentRepository {
  @override
  Future<StudentAttempt> createAttempt(int examId) =>
      throw UnimplementedError();

  @override
  Future<StudentAttempt> getAttempt(int attemptId) =>
      throw UnimplementedError();

  @override
  Future<List<StudentExamHistoryItem>> getMyAttemptHistory() async => const [];

  @override
  Future<StudentAttempt> saveAnswer({
    required int attemptId,
    required int questionId,
    required int? selectedOptionId,
  }) => throw UnimplementedError();

  @override
  Future<StudentExamResult> submitAttempt(int attemptId) =>
      throw UnimplementedError();

  @override
  Future<ExamDetail> getExamDetail(int examId) => throw UnimplementedError();

  @override
  Future<List<Exam>> getPublishedExams() async => const [];

  @override
  Future<StudentProfile?> restoreStudentSession() async => null;

  @override
  Future<void> signOut() async {}

  @override
  Future<StudentProfile> signIn(String username, String password) =>
      throw UnimplementedError();
}
