class Exam {
  const Exam({
    required this.id,
    required this.title,
    required this.description,
    required this.durationMinutes,
    required this.questionCount,
  });

  final int id;
  final String title;
  final String? description;
  final int durationMinutes;
  final int questionCount;

  factory Exam.fromJson(Map<String, dynamic> json) {
    return Exam(
      id: _readInt(json['id'], 'id'),
      title: _readString(json['title'], 'title'),
      description: _readNullableString(json['description'], 'description'),
      durationMinutes: _readInt(json['durationMinutes'], 'durationMinutes'),
      questionCount: _readInt(json['questionCount'], 'questionCount'),
    );
  }
}

class ExamDetail extends Exam {
  const ExamDetail({
    required super.id,
    required super.title,
    required super.description,
    required super.durationMinutes,
    required super.questionCount,
    required this.readingPassages,
    required this.parts,
  });

  final List<ReadingPassage> readingPassages;
  final List<int> parts;

  factory ExamDetail.fromJson(Map<String, dynamic> json) {
    final questions = _readList(json['questions'], 'questions');
    final passages = _readList(json['readingPassages'], 'readingPassages')
        .map((passage) => ReadingPassage.fromJson(_readMap(passage)))
        .toList(growable: false);
    final parsedQuestions = questions.map(_readMap).toList(growable: false);

    return ExamDetail(
      id: _readInt(json['id'], 'id'),
      title: _readString(json['title'], 'title'),
      description: _readNullableString(json['description'], 'description'),
      durationMinutes: _readInt(json['durationMinutes'], 'durationMinutes'),
      questionCount: parsedQuestions.length,
      readingPassages: passages,
      parts:
          parsedQuestions
              .map((question) => _readInt(question['partNumber'], 'partNumber'))
              .toSet()
              .toList()
            ..sort(),
    );
  }
}

class ReadingPassage {
  const ReadingPassage({
    required this.partNumber,
    required this.title,
    required this.content,
    required this.orderNumber,
  });

  final int partNumber;
  final String title;
  final String content;
  final int orderNumber;

  factory ReadingPassage.fromJson(Map<String, dynamic> json) {
    return ReadingPassage(
      partNumber: _readInt(json['partNumber'], 'partNumber'),
      title: _readString(json['title'], 'title'),
      content: _readString(json['content'], 'content'),
      orderNumber: _readInt(json['orderNumber'], 'orderNumber'),
    );
  }
}

List<dynamic> _readList(Object? value, String fieldName) {
  if (value is List) return value;
  throw FormatException('Trường $fieldName không hợp lệ từ API.');
}

Map<String, dynamic> _readMap(Object? value) {
  if (value is Map<String, dynamic>) return value;
  throw const FormatException('Bản ghi đề thi không hợp lệ từ API.');
}

int _readInt(Object? value, String fieldName) {
  if (value is int) return value;
  if (value is num) return value.toInt();
  throw FormatException('Trường $fieldName không hợp lệ từ API.');
}

String _readString(Object? value, String fieldName) {
  if (value is String) return value;
  throw FormatException('Trường $fieldName không hợp lệ từ API.');
}

String? _readNullableString(Object? value, String fieldName) {
  if (value == null) return null;
  if (value is String) return value;
  throw FormatException('Trường $fieldName không hợp lệ từ API.');
}
