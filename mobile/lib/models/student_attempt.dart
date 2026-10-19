class StudentAttempt {
  const StudentAttempt({
    required this.attemptId,
    required this.examId,
    required this.examTitle,
    required this.startedAtUtc,
    required this.expiresAtUtc,
    required this.remainingSeconds,
    required this.isSubmitted,
    required this.readingPassages,
    required this.questions,
    required this.answers,
    this.result,
  });

  final int attemptId;
  final int examId;
  final String examTitle;
  final DateTime startedAtUtc;
  final DateTime expiresAtUtc;
  final int remainingSeconds;
  final bool isSubmitted;
  final List<AttemptPassage> readingPassages;
  final List<AttemptQuestion> questions;
  final Map<int, int> answers;
  final StudentExamResult? result;

  factory StudentAttempt.fromJson(Map<String, dynamic> json) {
    final answers = <int, int>{};
    for (final item in _mapList(json['answers'], 'answers')) {
      final selectedOptionId = item['selectedOptionId'];
      if (selectedOptionId is num) {
        answers[_int(item['questionId'], 'questionId')] =
            selectedOptionId.toInt();
      }
    }

    final resultJson = json['result'];
    return StudentAttempt(
      attemptId: _int(json['attemptId'], 'attemptId'),
      examId: _int(json['examId'], 'examId'),
      examTitle: _string(json['examTitle'], 'examTitle'),
      startedAtUtc: _date(json['startedAtUtc'], 'startedAtUtc'),
      expiresAtUtc: _date(json['expiresAtUtc'], 'expiresAtUtc'),
      remainingSeconds: _int(json['remainingSeconds'], 'remainingSeconds'),
      isSubmitted: json['isSubmitted'] == true,
      readingPassages: _mapList(json['readingPassages'], 'readingPassages')
          .map(AttemptPassage.fromJson)
          .toList(growable: false),
      questions: _mapList(json['questions'], 'questions')
          .map(AttemptQuestion.fromJson)
          .toList(growable: false),
      answers: Map.unmodifiable(answers),
      result: resultJson is Map
          ? StudentExamResult.fromJson(Map<String, dynamic>.from(resultJson))
          : null,
    );
  }
}

class AttemptPassage {
  const AttemptPassage({
    required this.id,
    required this.partNumber,
    required this.title,
    required this.content,
    required this.orderNumber,
  });

  final int id;
  final int partNumber;
  final String title;
  final String content;
  final int orderNumber;

  factory AttemptPassage.fromJson(Map<String, dynamic> json) {
    return AttemptPassage(
      id: _int(json['id'], 'id'),
      partNumber: _int(json['partNumber'], 'partNumber'),
      title: _string(json['title'], 'title'),
      content: _string(json['content'], 'content'),
      orderNumber: _int(json['orderNumber'], 'orderNumber'),
    );
  }
}

class AttemptQuestion {
  const AttemptQuestion({
    required this.id,
    required this.readingPassageId,
    required this.partNumber,
    required this.questionNumber,
    required this.questionText,
    required this.options,
  });

  final int id;
  final int? readingPassageId;
  final int partNumber;
  final int questionNumber;
  final String questionText;
  final List<AttemptOption> options;

  factory AttemptQuestion.fromJson(Map<String, dynamic> json) {
    final passageId = json['readingPassageId'];
    return AttemptQuestion(
      id: _int(json['id'], 'id'),
      readingPassageId: passageId is num ? passageId.toInt() : null,
      partNumber: _int(json['partNumber'], 'partNumber'),
      questionNumber: _int(json['questionNumber'], 'questionNumber'),
      questionText: _string(json['questionText'], 'questionText'),
      options: _mapList(json['options'], 'options')
          .map(AttemptOption.fromJson)
          .toList(growable: false),
    );
  }
}

class AttemptOption {
  const AttemptOption({
    required this.id,
    required this.label,
    required this.content,
  });

  final int id;
  final String label;
  final String content;

  factory AttemptOption.fromJson(Map<String, dynamic> json) {
    return AttemptOption(
      id: _int(json['id'], 'id'),
      label: _string(json['label'], 'label'),
      content: _string(json['content'], 'content'),
    );
  }
}

class StudentExamResult {
  const StudentExamResult({
    required this.attemptId,
    required this.examId,
    required this.examTitle,
    required this.startedAtUtc,
    required this.submittedAtUtc,
    required this.durationSeconds,
    required this.score,
    required this.correctCount,
    required this.wrongCount,
    required this.blankCount,
    required this.totalQuestionCount,
    required this.accuracyPercent,
  });

  final int attemptId;
  final int examId;
  final String examTitle;
  final DateTime startedAtUtc;
  final DateTime submittedAtUtc;
  final int durationSeconds;
  final int score;
  final int correctCount;
  final int wrongCount;
  final int blankCount;
  final int totalQuestionCount;
  final double accuracyPercent;

  factory StudentExamResult.fromJson(Map<String, dynamic> json) {
    return StudentExamResult(
      attemptId: _int(json['attemptId'], 'attemptId'),
      examId: _int(json['examId'], 'examId'),
      examTitle: _string(json['examTitle'], 'examTitle'),
      startedAtUtc: _date(json['startedAtUtc'], 'startedAtUtc'),
      submittedAtUtc: _date(json['submittedAtUtc'], 'submittedAtUtc'),
      durationSeconds: _int(json['durationSeconds'], 'durationSeconds'),
      score: _int(json['score'], 'score'),
      correctCount: _int(json['correctCount'], 'correctCount'),
      wrongCount: _int(json['wrongCount'], 'wrongCount'),
      blankCount: _int(json['blankCount'], 'blankCount'),
      totalQuestionCount: _int(json['totalQuestionCount'], 'totalQuestionCount'),
      accuracyPercent: _double(json['accuracyPercent'], 'accuracyPercent'),
    );
  }
}

class StudentExamHistoryItem extends StudentExamResult {
  const StudentExamHistoryItem({
    required super.attemptId,
    required super.examId,
    required super.examTitle,
    required super.startedAtUtc,
    required super.submittedAtUtc,
    required super.durationSeconds,
    required super.score,
    required super.correctCount,
    required super.wrongCount,
    required super.blankCount,
    required super.totalQuestionCount,
    required super.accuracyPercent,
  });

  factory StudentExamHistoryItem.fromJson(Map<String, dynamic> json) {
    return StudentExamHistoryItem(
      attemptId: _int(json['attemptId'], 'attemptId'),
      examId: _int(json['examId'], 'examId'),
      examTitle: _string(json['examTitle'], 'examTitle'),
      startedAtUtc: _date(json['startedAtUtc'], 'startedAtUtc'),
      submittedAtUtc: _date(json['submittedAtUtc'], 'submittedAtUtc'),
      durationSeconds: _int(json['durationSeconds'], 'durationSeconds'),
      score: _int(json['score'], 'score'),
      correctCount: _int(json['correctCount'], 'correctCount'),
      wrongCount: _int(json['wrongCount'], 'wrongCount'),
      blankCount: _int(json['blankCount'], 'blankCount'),
      totalQuestionCount: _int(json['totalQuestionCount'], 'totalQuestionCount'),
      accuracyPercent: _double(json['accuracyPercent'], 'accuracyPercent'),
    );
  }
}

List<Map<String, dynamic>> _mapList(Object? value, String fieldName) {
  if (value is! List) {
    throw FormatException('Trường $fieldName không hợp lệ từ API.');
  }
  return value.map((item) {
    if (item is Map) return Map<String, dynamic>.from(item);
    throw FormatException('Bản ghi trong $fieldName không hợp lệ từ API.');
  }).toList(growable: false);
}

int _int(Object? value, String fieldName) {
  if (value is num) return value.toInt();
  throw FormatException('Trường $fieldName không hợp lệ từ API.');
}

double _double(Object? value, String fieldName) {
  if (value is num) return value.toDouble();
  throw FormatException('Trường $fieldName không hợp lệ từ API.');
}

String _string(Object? value, String fieldName) {
  if (value is String) return value;
  throw FormatException('Trường $fieldName không hợp lệ từ API.');
}

DateTime _date(Object? value, String fieldName) {
  if (value is String) {
    final date = DateTime.tryParse(value);
    if (date != null) return date.toUtc();
  }
  throw FormatException('Trường $fieldName không hợp lệ từ API.');
}
