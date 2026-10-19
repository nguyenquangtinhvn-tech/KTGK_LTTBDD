import 'package:flutter/material.dart';

import '../models/student_attempt.dart';
import '../models/exam.dart';
import '../repositories/student_repository.dart';
import 'exam_detail_screen.dart';

class ExamResultScreen extends StatelessWidget {
  const ExamResultScreen({
    required this.result,
    this.repository,
    this.exam,
    this.warningMessage,
    super.key,
  });

  final StudentExamResult result;
  final StudentRepository? repository;
  final Exam? exam;
  final String? warningMessage;

  @override
  Widget build(BuildContext context) {
    final minutes = result.durationSeconds ~/ 60;
    final seconds = result.durationSeconds % 60;
    return Scaffold(
      appBar: AppBar(
        title: const Text('Kết quả bài thi'),
      ),
      body: SafeArea(
        child: ListView(
          padding: const EdgeInsets.all(20),
          children: [
            Container(
              padding: const EdgeInsets.all(24),
              decoration: BoxDecoration(
                gradient: const LinearGradient(
                  colors: [Color(0xFF2864E8), Color(0xFF5790FF)],
                ),
                borderRadius: BorderRadius.circular(24),
              ),
              child: Column(
                children: [
                  const Icon(
                    Icons.emoji_events_rounded,
                    color: Colors.white,
                    size: 48,
                  ),
                  const SizedBox(height: 12),
                  Text(
                    result.examTitle,
                    textAlign: TextAlign.center,
                    style: const TextStyle(
                      color: Colors.white,
                      fontSize: 20,
                      fontWeight: FontWeight.w800,
                    ),
                  ),
                  const SizedBox(height: 8),
                  Text(
                    '${result.score}/${result.totalQuestionCount} câu đúng',
                    style: const TextStyle(
                      color: Colors.white,
                      fontSize: 30,
                      fontWeight: FontWeight.w900,
                    ),
                  ),
                  Text(
                    'Độ chính xác ${result.accuracyPercent.toStringAsFixed(1)}%',
                    style: const TextStyle(color: Colors.white70),
                  ),
                ],
              ),
            ),
            const SizedBox(height: 16),
            Card(
              child: Column(
                children: [
                  _ResultMetric(
                    label: 'Câu đúng',
                    value: '${result.correctCount}',
                    color: const Color(0xFF13845F),
                  ),
                  const Divider(height: 1),
                  _ResultMetric(
                    label: 'Câu sai',
                    value: '${result.wrongCount}',
                    color: const Color(0xFFB42318),
                  ),
                  const Divider(height: 1),
                  _ResultMetric(
                    label: 'Bỏ trống',
                    value: '${result.blankCount}',
                    color: const Color(0xFF68758A),
                  ),
                  const Divider(height: 1),
                  _ResultMetric(
                    label: 'Thời gian làm bài',
                    value: '$minutes phút ${seconds.toString().padLeft(2, '0')} giây',
                    color: const Color(0xFF2864E8),
                  ),
                ],
              ),
            ),
            const SizedBox(height: 12),
            if (warningMessage != null) ...[
              Container(
                padding: const EdgeInsets.all(14),
                decoration: BoxDecoration(
                  color: const Color(0xFFFFF7E6),
                  borderRadius: BorderRadius.circular(15),
                ),
                child: Text(
                  warningMessage!,
                  style: const TextStyle(
                    color: Color(0xFF7C5309),
                    height: 1.4,
                  ),
                ),
              ),
              const SizedBox(height: 12),
            ],
            const Text(
              'Điểm đang hiển thị là số câu đúng theo dữ liệu chấm từ máy chủ. '
              'Đáp án chuẩn và lời giải chưa được mở cho ứng dụng.',
              style: TextStyle(color: Color(0xFF68758A), height: 1.45),
              textAlign: TextAlign.center,
            ),
            const SizedBox(height: 22),
            if (exam != null && repository != null)
              FilledButton.icon(
                onPressed: () => Navigator.of(context).pushReplacement(
                  MaterialPageRoute<void>(
                    builder: (_) => ExamDetailScreen(
                      exam: exam!,
                      repository: repository!,
                    ),
                  ),
                ),
                icon: const Icon(Icons.replay_rounded),
                label: const Text('Làm lại đề thi'),
              ),
            OutlinedButton(
              onPressed: () =>
                  Navigator.of(context).popUntil((route) => route.isFirst),
              child: const Text('Về trang chủ'),
            ),
          ],
        ),
      ),
    );
  }
}

class _ResultMetric extends StatelessWidget {
  const _ResultMetric({
    required this.label,
    required this.value,
    required this.color,
  });

  final String label;
  final String value;
  final Color color;

  @override
  Widget build(BuildContext context) => Padding(
    padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
    child: Row(
      children: [
        Expanded(child: Text(label)),
        Text(
          value,
          style: TextStyle(color: color, fontWeight: FontWeight.w800),
        ),
      ],
    ),
  );
}
