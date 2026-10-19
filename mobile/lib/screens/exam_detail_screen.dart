import 'package:flutter/material.dart';

import '../models/exam.dart';
import '../repositories/student_repository.dart';
import '../services/api_client.dart';
import 'exam_attempt_screen.dart';
import 'exam_result_screen.dart';

class ExamDetailScreen extends StatefulWidget {
  const ExamDetailScreen({
    required this.exam,
    required this.repository,
    super.key,
  });

  final Exam exam;
  final StudentRepository repository;

  @override
  State<ExamDetailScreen> createState() => _ExamDetailScreenState();
}

class _ExamDetailScreenState extends State<ExamDetailScreen> {
  late Future<ExamDetail> _detailFuture;
  bool _startingAttempt = false;
  Object? _startError;

  @override
  void initState() {
    super.initState();
    _detailFuture = widget.repository.getExamDetail(widget.exam.id);
  }

  Future<void> _reload() async {
    final future = widget.repository.getExamDetail(widget.exam.id);
    setState(() => _detailFuture = future);
    await future.then<void>((_) {}, onError: (Object _) {});
  }

  Future<void> _startAttempt() async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Bắt đầu làm bài?'),
        content: const Text(
          'Thời gian sẽ bắt đầu tính ngay khi máy chủ tạo lượt thi. '
          'Nếu bạn đã có lượt thi đang làm, ứng dụng sẽ tiếp tục lượt đó.',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, false),
            child: const Text('Để sau'),
          ),
          FilledButton(
            onPressed: () => Navigator.pop(context, true),
            child: const Text('Bắt đầu'),
          ),
        ],
      ),
    );
    if (confirmed != true || !mounted) return;

    setState(() {
      _startingAttempt = true;
      _startError = null;
    });
    try {
      final attempt = await widget.repository.createAttempt(widget.exam.id);
      if (!mounted) return;
      if (attempt.isSubmitted && attempt.result != null) {
        await Navigator.of(context).push(
          MaterialPageRoute<void>(
            builder: (_) => ExamResultScreen(
              result: attempt.result!,
              repository: widget.repository,
              exam: widget.exam,
            ),
          ),
        );
      } else {
        await Navigator.of(context).push(
          MaterialPageRoute<void>(
            builder: (_) => ExamAttemptScreen(
              attempt: attempt,
              exam: widget.exam,
              repository: widget.repository,
            ),
          ),
        );
      }
    } catch (error) {
      if (!mounted) return;
      setState(() => _startError = error);
    } finally {
      if (mounted) setState(() => _startingAttempt = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Scaffold(
      appBar: AppBar(title: const Text('Chi tiết đề thi')),
      body: SafeArea(
        child: FutureBuilder<ExamDetail>(
          future: _detailFuture,
          builder: (context, snapshot) {
            if (snapshot.hasError) {
              return Center(
                child: Padding(
                  padding: const EdgeInsets.all(24),
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Text(
                        errorMessage(snapshot.error!),
                        textAlign: TextAlign.center,
                      ),
                      const SizedBox(height: 12),
                      OutlinedButton.icon(
                        onPressed: _reload,
                        icon: const Icon(Icons.refresh_rounded),
                        label: const Text('Thử lại'),
                      ),
                    ],
                  ),
                ),
              );
            }
            if (snapshot.connectionState != ConnectionState.done) {
              return const Center(child: CircularProgressIndicator());
            }

            final detail = snapshot.data!;
            return ListView(
              padding: const EdgeInsets.fromLTRB(20, 12, 20, 24),
              children: [
                Container(
                  padding: const EdgeInsets.all(20),
                  decoration: BoxDecoration(
                    color: Colors.white,
                    borderRadius: BorderRadius.circular(22),
                    border: Border.all(color: const Color(0xFFE8EDF5)),
                  ),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      const _PublishedLabel(),
                      const SizedBox(height: 13),
                      Text(
                        detail.title,
                        style: theme.textTheme.headlineSmall?.copyWith(
                          fontWeight: FontWeight.w800,
                        ),
                      ),
                      if (detail.description != null &&
                          detail.description!.trim().isNotEmpty) ...[
                        const SizedBox(height: 10),
                        Text(
                          detail.description!,
                          style: theme.textTheme.bodyMedium?.copyWith(
                            color: const Color(0xFF5F6D82),
                            height: 1.45,
                          ),
                        ),
                      ],
                      const SizedBox(height: 18),
                      Row(
                        children: [
                          Expanded(
                            child: _DetailMetric(
                              icon: Icons.schedule_rounded,
                              value: '${detail.durationMinutes} phút',
                              label: 'Thời lượng',
                            ),
                          ),
                          Expanded(
                            child: _DetailMetric(
                              icon: Icons.quiz_outlined,
                              value: '${detail.questionCount} câu',
                              label: 'Số câu hỏi',
                            ),
                          ),
                        ],
                      ),
                      if (detail.parts.isNotEmpty) ...[
                        const SizedBox(height: 18),
                        Text(
                          'Phần thi: ${detail.parts.map((part) => 'Part $part').join(' · ')}',
                          style: const TextStyle(
                            color: Color(0xFF47566D),
                            fontWeight: FontWeight.w600,
                          ),
                        ),
                      ],
                    ],
                  ),
                ),
                const SizedBox(height: 20),
                Text(
                  'Bài đọc',
                  style: theme.textTheme.titleLarge?.copyWith(
                    fontWeight: FontWeight.w700,
                  ),
                ),
                const SizedBox(height: 10),
                if (detail.readingPassages.isEmpty)
                  const _InformationCard(
                    icon: Icons.menu_book_outlined,
                    message: 'Đề thi không có bài đọc được trả về.',
                  )
                else
                  ...detail.readingPassages.map(
                    (passage) => Card(
                      margin: const EdgeInsets.only(bottom: 10),
                      child: ExpansionTile(
                        leading: const Icon(
                          Icons.article_outlined,
                          color: Color(0xFF2864E8),
                        ),
                        title: Text(
                          passage.title.isEmpty
                              ? 'Part ${passage.partNumber} · Bài đọc'
                              : passage.title,
                          maxLines: 2,
                          overflow: TextOverflow.ellipsis,
                        ),
                        subtitle: Text('Part ${passage.partNumber}'),
                        childrenPadding: const EdgeInsets.fromLTRB(
                          16,
                          0,
                          16,
                          16,
                        ),
                        children: [
                          Align(
                            alignment: Alignment.centerLeft,
                            child: Text(
                              passage.content,
                              style: const TextStyle(height: 1.5),
                            ),
                          ),
                        ],
                      ),
                    ),
                  ),
                const SizedBox(height: 16),
                const _InformationCard(
                  icon: Icons.info_outline_rounded,
                  message:
                      'Câu trả lời và thời gian làm bài được xác nhận, lưu và '
                      'chấm bởi máy chủ. Đáp án đúng và lời giải không hiển thị '
                      'trong khi thi.',
                ),
                if (_startError != null) ...[
                  const SizedBox(height: 10),
                  _InformationCard(
                    icon: Icons.error_outline_rounded,
                    message: errorMessage(_startError!),
                  ),
                ],
                const SizedBox(height: 14),
                SizedBox(
                  height: 52,
                  child: FilledButton.icon(
                    onPressed: _startingAttempt ? null : _startAttempt,
                    icon: _startingAttempt
                        ? const SizedBox(
                            width: 18,
                            height: 18,
                            child: CircularProgressIndicator(strokeWidth: 2),
                          )
                        : const Icon(Icons.play_arrow_rounded),
                    label: Text(
                      _startingAttempt ? 'Đang kết nối…' : 'Bắt đầu làm bài',
                    ),
                  ),
                ),
              ],
            );
          },
        ),
      ),
    );
  }
}

class _DetailMetric extends StatelessWidget {
  const _DetailMetric({
    required this.icon,
    required this.value,
    required this.label,
  });

  final IconData icon;
  final String value;
  final String label;

  @override
  Widget build(BuildContext context) => Row(
    children: [
      CircleAvatar(
        radius: 19,
        backgroundColor: const Color(0xFFEAF1FF),
        child: Icon(icon, color: const Color(0xFF2864E8), size: 20),
      ),
      const SizedBox(width: 9),
      Flexible(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              value,
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
              style: const TextStyle(fontWeight: FontWeight.w700),
            ),
            Text(
              label,
              style: const TextStyle(fontSize: 12, color: Color(0xFF68758A)),
            ),
          ],
        ),
      ),
    ],
  );
}

class _PublishedLabel extends StatelessWidget {
  const _PublishedLabel();

  @override
  Widget build(BuildContext context) => const Chip(
    avatar: Icon(Icons.check_circle_rounded, size: 17),
    label: Text('Đã xuất bản'),
    visualDensity: VisualDensity.compact,
    side: BorderSide.none,
    backgroundColor: Color(0xFFE7F7F1),
    labelStyle: TextStyle(
      color: Color(0xFF13845F),
      fontWeight: FontWeight.w700,
    ),
  );
}

class _InformationCard extends StatelessWidget {
  const _InformationCard({required this.icon, required this.message});

  final IconData icon;
  final String message;

  @override
  Widget build(BuildContext context) => Container(
    padding: const EdgeInsets.all(14),
    decoration: BoxDecoration(
      color: const Color(0xFFFFF7E6),
      borderRadius: BorderRadius.circular(15),
    ),
    child: Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Icon(icon, color: const Color(0xFFAA6800), size: 20),
        const SizedBox(width: 9),
        Expanded(
          child: Text(
            message,
            style: const TextStyle(color: Color(0xFF7C5309), height: 1.4),
          ),
        ),
      ],
    ),
  );
}
