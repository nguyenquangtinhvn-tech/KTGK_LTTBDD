import 'package:flutter/material.dart';

import '../models/student_attempt.dart';
import '../repositories/student_repository.dart';
import '../services/api_client.dart';
import 'exam_result_screen.dart';

class HistoryScreen extends StatefulWidget {
  const HistoryScreen({required this.repository, super.key});

  final StudentRepository repository;

  @override
  State<HistoryScreen> createState() => _HistoryScreenState();
}

class _HistoryScreenState extends State<HistoryScreen> {
  late Future<List<StudentExamHistoryItem>> _historyFuture;

  @override
  void initState() {
    super.initState();
    _historyFuture = widget.repository.getMyAttemptHistory();
  }

  Future<void> _reload() async {
    final future = widget.repository.getMyAttemptHistory();
    setState(() => _historyFuture = future);
    await future.then<void>((_) {}, onError: (Object _) {});
  }

  Future<void> _showResult(StudentExamHistoryItem item) async {
    await Navigator.of(context).push(
      MaterialPageRoute<void>(
        builder: (_) => ExamResultScreen(result: item),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return SafeArea(
      child: RefreshIndicator(
        onRefresh: _reload,
        child: FutureBuilder<List<StudentExamHistoryItem>>(
          future: _historyFuture,
          builder: (context, snapshot) {
            if (snapshot.hasError) {
              return ListView(
                physics: const AlwaysScrollableScrollPhysics(),
                padding: const EdgeInsets.all(24),
                children: [
                  const SizedBox(height: 100),
                  const Icon(
                    Icons.cloud_off_rounded,
                    size: 44,
                    color: Color(0xFF2864E8),
                  ),
                  const SizedBox(height: 14),
                  Text(
                    errorMessage(snapshot.error!),
                    textAlign: TextAlign.center,
                  ),
                  const SizedBox(height: 12),
                  Center(
                    child: OutlinedButton.icon(
                      onPressed: _reload,
                      icon: const Icon(Icons.refresh_rounded),
                      label: const Text('Thử lại'),
                    ),
                  ),
                ],
              );
            }
            if (snapshot.connectionState != ConnectionState.done) {
              return const Center(child: CircularProgressIndicator());
            }

            final history = snapshot.data!;
            return ListView(
              physics: const AlwaysScrollableScrollPhysics(),
              padding: const EdgeInsets.fromLTRB(20, 20, 20, 24),
              children: [
                Text(
                  'Lịch sử thi',
                  style: Theme.of(context).textTheme.headlineSmall?.copyWith(
                    fontWeight: FontWeight.w800,
                  ),
                ),
                const SizedBox(height: 6),
                const Text(
                  'Kết quả từ các lượt thi của tài khoản hiện tại.',
                  style: TextStyle(color: Color(0xFF68758A)),
                ),
                const SizedBox(height: 18),
                if (history.isEmpty)
                  const _EmptyHistory()
                else
                  ...history.map(
                    (item) => Card(
                      margin: const EdgeInsets.only(bottom: 12),
                      child: InkWell(
                        borderRadius: BorderRadius.circular(20),
                        onTap: () => _showResult(item),
                        child: Padding(
                          padding: const EdgeInsets.all(16),
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Row(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  const CircleAvatar(
                                    backgroundColor: Color(0xFFEAF1FF),
                                    child: Icon(
                                      Icons.assignment_turned_in_outlined,
                                      color: Color(0xFF2864E8),
                                    ),
                                  ),
                                  const SizedBox(width: 12),
                                  Expanded(
                                    child: Column(
                                      crossAxisAlignment:
                                          CrossAxisAlignment.start,
                                      children: [
                                        Text(
                                          item.examTitle,
                                          style: const TextStyle(
                                            fontWeight: FontWeight.w700,
                                            fontSize: 16,
                                          ),
                                        ),
                                        const SizedBox(height: 4),
                                        Text(
                                          _formatDate(item.submittedAtUtc),
                                          style: const TextStyle(
                                            color: Color(0xFF68758A),
                                            fontSize: 13,
                                          ),
                                        ),
                                      ],
                                    ),
                                  ),
                                  const Icon(
                                    Icons.chevron_right_rounded,
                                    color: Color(0xFF68758A),
                                  ),
                                ],
                              ),
                              const SizedBox(height: 14),
                              Row(
                                children: [
                                  Expanded(
                                    child: _HistoryMetric(
                                      label: 'Điểm thô',
                                      value:
                                          '${item.score}/${item.totalQuestionCount}',
                                    ),
                                  ),
                                  Expanded(
                                    child: _HistoryMetric(
                                      label: 'Câu đúng',
                                      value: '${item.correctCount}',
                                    ),
                                  ),
                                  Expanded(
                                    child: _HistoryMetric(
                                      label: 'Thời gian',
                                      value: '${item.durationSeconds ~/ 60} phút',
                                    ),
                                  ),
                                ],
                              ),
                            ],
                          ),
                        ),
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

String _formatDate(DateTime value) {
  final local = value.toLocal();
  return '${local.day.toString().padLeft(2, '0')}/'
      '${local.month.toString().padLeft(2, '0')}/${local.year} · '
      '${local.hour.toString().padLeft(2, '0')}:'
      '${local.minute.toString().padLeft(2, '0')}';
}

class _HistoryMetric extends StatelessWidget {
  const _HistoryMetric({required this.label, required this.value});

  final String label;
  final String value;

  @override
  Widget build(BuildContext context) => Column(
    crossAxisAlignment: CrossAxisAlignment.start,
    children: [
      Text(
        value,
        style: const TextStyle(
          color: Color(0xFF17233B),
          fontWeight: FontWeight.w800,
        ),
      ),
      const SizedBox(height: 2),
      Text(
        label,
        style: const TextStyle(color: Color(0xFF68758A), fontSize: 12),
      ),
    ],
  );
}

class _EmptyHistory extends StatelessWidget {
  const _EmptyHistory();

  @override
  Widget build(BuildContext context) => Padding(
    padding: const EdgeInsets.only(top: 80),
    child: Column(
      children: [
        Container(
          width: 76,
          height: 76,
          decoration: BoxDecoration(
            color: const Color(0xFFEAF1FF),
            borderRadius: BorderRadius.circular(24),
          ),
          child: const Icon(
            Icons.history_rounded,
            size: 38,
            color: Color(0xFF2864E8),
          ),
        ),
        const SizedBox(height: 18),
        Text(
          'Chưa có lượt thi hoàn thành',
          style: Theme.of(context).textTheme.titleLarge?.copyWith(
            fontWeight: FontWeight.w700,
          ),
          textAlign: TextAlign.center,
        ),
        const SizedBox(height: 8),
        const Text(
          'Kết quả của bạn sẽ xuất hiện tại đây sau khi nộp bài.',
          textAlign: TextAlign.center,
          style: TextStyle(color: Color(0xFF68758A), height: 1.45),
        ),
      ],
    ),
  );
}
