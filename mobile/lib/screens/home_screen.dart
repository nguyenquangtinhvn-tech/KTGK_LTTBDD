import 'package:flutter/material.dart';

import '../models/exam.dart';
import '../models/student_attempt.dart';
import '../models/student_profile.dart';
import '../repositories/student_repository.dart';
import '../services/api_client.dart';
import 'exam_detail_screen.dart';
import 'exam_list_screen.dart';

class HomeScreen extends StatefulWidget {
  const HomeScreen({
    required this.profile,
    required this.repository,
    required this.onBrowseExams,
    super.key,
  });

  final StudentProfile profile;
  final StudentRepository repository;
  final VoidCallback onBrowseExams;

  @override
  State<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends State<HomeScreen> {
  late Future<List<Exam>> _examsFuture;
  late Future<List<StudentExamHistoryItem>> _historyFuture;

  @override
  void initState() {
    super.initState();
    _examsFuture = widget.repository.getPublishedExams();
    _historyFuture = widget.repository.getMyAttemptHistory();
  }

  Future<void> _reload() async {
    final future = widget.repository.getPublishedExams();
    final historyFuture = widget.repository.getMyAttemptHistory();
    setState(() {
      _examsFuture = future;
      _historyFuture = historyFuture;
    });
    await Future.wait<void>([
      future.then<void>((_) {}, onError: (Object _) {}),
      historyFuture.then<void>((_) {}, onError: (Object _) {}),
    ]);
  }

  Future<void> _openExam(Exam exam) async {
    await Navigator.of(context).push(
      MaterialPageRoute<void>(
        builder: (_) =>
            ExamDetailScreen(exam: exam, repository: widget.repository),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return SafeArea(
      child: RefreshIndicator(
        onRefresh: _reload,
        child: CustomScrollView(
          physics: const AlwaysScrollableScrollPhysics(),
          slivers: [
            SliverPadding(
              padding: const EdgeInsets.fromLTRB(20, 16, 20, 24),
              sliver: SliverList(
                delegate: SliverChildListDelegate([
                  Row(
                    children: [
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(
                              'Xin chào,',
                              style: theme.textTheme.bodyLarge?.copyWith(
                                color: const Color(0xFF68758A),
                              ),
                            ),
                            const SizedBox(height: 3),
                            Text(
                              widget.profile.displayName,
                              maxLines: 1,
                              overflow: TextOverflow.ellipsis,
                              style: theme.textTheme.headlineSmall?.copyWith(
                                fontWeight: FontWeight.w800,
                                color: const Color(0xFF17233B),
                              ),
                            ),
                          ],
                        ),
                      ),
                      const CircleAvatar(
                        radius: 23,
                        backgroundColor: Color(0xFFE6EEFF),
                        child: Icon(
                          Icons.person_rounded,
                          color: Color(0xFF2864E8),
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 22),
                  _PracticeBanner(onPressed: widget.onBrowseExams),
                  const SizedBox(height: 24),
                  Text(
                    'Tổng quan luyện tập',
                    style: theme.textTheme.titleLarge?.copyWith(
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                  const SizedBox(height: 12),
                  FutureBuilder<List<Exam>>(
                    future: _examsFuture,
                    builder: (context, snapshot) {
                      if (snapshot.hasError) {
                        return _HomeLoadError(
                          error: snapshot.error!,
                          onRetry: _reload,
                        );
                      }
                      if (snapshot.connectionState != ConnectionState.done) {
                        return const Padding(
                          padding: EdgeInsets.symmetric(vertical: 26),
                          child: Center(child: CircularProgressIndicator()),
                        );
                      }

                      final exams = snapshot.data!;
                      return Column(
                        children: [
                          FutureBuilder<List<StudentExamHistoryItem>>(
                            future: _historyFuture,
                            builder: (context, historySnapshot) {
                              final history = historySnapshot.data ?? const [];
                              final latest = history.isEmpty ? null : history.first;
                              return Column(
                                children: [
                                  Row(
                                    children: [
                                      Expanded(
                                        child: _MetricCard(
                                          icon: Icons.library_books_rounded,
                                          label: 'Đề đang mở',
                                          value: '${exams.length}',
                                          tint: const Color(0xFFE8F0FF),
                                          iconColor: const Color(0xFF2864E8),
                                        ),
                                      ),
                                      const SizedBox(width: 12),
                                      Expanded(
                                        child: _MetricCard(
                                          icon: Icons.check_circle_outline_rounded,
                                          label: 'Bài hoàn thành',
                                          value: historySnapshot.hasError
                                              ? 'Chưa tải được'
                                              : historySnapshot.connectionState !=
                                                    ConnectionState.done
                                              ? '…'
                                              : '${history.length}',
                                          tint: const Color(0xFFE7F7F1),
                                          iconColor: const Color(0xFF14956D),
                                        ),
                                      ),
                                    ],
                                  ),
                                  const SizedBox(height: 12),
                                  _DataNote(
                                    text: historySnapshot.hasError
                                        ? 'Không tải được điểm gần nhất: '
                                              '${errorMessage(historySnapshot.error!)}'
                                        : latest == null
                                        ? 'Chưa có lượt thi hoàn thành để hiển thị điểm gần nhất.'
                                        : 'Điểm gần nhất: ${latest.score}/'
                                              '${latest.totalQuestionCount} câu đúng · '
                                              '${latest.examTitle}',
                                  ),
                                ],
                              );
                            },
                          ),
                          const SizedBox(height: 26),
                          _SectionHeader(
                            title: 'Đề thi nổi bật',
                            action: 'Xem tất cả',
                            onPressed: widget.onBrowseExams,
                          ),
                          const SizedBox(height: 10),
                          if (exams.isEmpty)
                            const _EmptyExams()
                          else
                            ...exams
                                .take(3)
                                .map(
                                  (exam) => Padding(
                                    padding: const EdgeInsets.only(bottom: 10),
                                    child: ExamCard(
                                      exam: exam,
                                      onTap: () => _openExam(exam),
                                    ),
                                  ),
                                ),
                        ],
                      );
                    },
                  ),
                ]),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _PracticeBanner extends StatelessWidget {
  const _PracticeBanner({required this.onPressed});

  final VoidCallback onPressed;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(20),
      decoration: BoxDecoration(
        borderRadius: BorderRadius.circular(22),
        gradient: const LinearGradient(
          colors: [Color(0xFF2864E8), Color(0xFF5790FF)],
          begin: Alignment.topLeft,
          end: Alignment.bottomRight,
        ),
      ),
      child: Row(
        children: [
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Text(
                  'Sẵn sàng luyện tập?',
                  style: TextStyle(
                    color: Colors.white,
                    fontSize: 19,
                    fontWeight: FontWeight.w800,
                  ),
                ),
                const SizedBox(height: 7),
                const Text(
                  'Khám phá các đề Reading đã được xuất bản.',
                  style: TextStyle(color: Colors.white70, height: 1.35),
                ),
                const SizedBox(height: 16),
                FilledButton.tonal(
                  onPressed: onPressed,
                  style: FilledButton.styleFrom(
                    backgroundColor: Colors.white,
                    foregroundColor: const Color(0xFF2864E8),
                  ),
                  child: const Text('Khám phá đề thi'),
                ),
              ],
            ),
          ),
          const SizedBox(width: 10),
          const Icon(
            Icons.auto_stories_rounded,
            color: Colors.white70,
            size: 48,
          ),
        ],
      ),
    );
  }
}

class _MetricCard extends StatelessWidget {
  const _MetricCard({
    required this.icon,
    required this.label,
    required this.value,
    required this.tint,
    required this.iconColor,
  });

  final IconData icon;
  final String label;
  final String value;
  final Color tint;
  final Color iconColor;

  @override
  Widget build(BuildContext context) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(15),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            CircleAvatar(
              radius: 19,
              backgroundColor: tint,
              child: Icon(icon, color: iconColor, size: 20),
            ),
            const SizedBox(height: 14),
            Text(
              value,
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
              style: Theme.of(context).textTheme.titleMedium
                  ?.copyWith(fontWeight: FontWeight.w800),
            ),
            const SizedBox(height: 3),
            Text(
              label,
              style: Theme.of(context).textTheme.bodySmall
                  ?.copyWith(color: const Color(0xFF68758A)),
            ),
          ],
        ),
      ),
    );
  }
}

class _SectionHeader extends StatelessWidget {
  const _SectionHeader({
    required this.title,
    required this.action,
    required this.onPressed,
  });

  final String title;
  final String action;
  final VoidCallback onPressed;

  @override
  Widget build(BuildContext context) {
    return Row(
      children: [
        Expanded(
          child: Text(
            title,
            style: Theme.of(context).textTheme.titleLarge
                ?.copyWith(fontWeight: FontWeight.w700),
          ),
        ),
        TextButton(onPressed: onPressed, child: Text(action)),
      ],
    );
  }
}

class _HomeLoadError extends StatelessWidget {
  const _HomeLoadError({required this.error, required this.onRetry});

  final Object error;
  final Future<void> Function() onRetry;

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        Text(errorMessage(error), textAlign: TextAlign.center),
        const SizedBox(height: 8),
        TextButton.icon(
          onPressed: onRetry,
          icon: const Icon(Icons.refresh_rounded),
          label: const Text('Thử lại'),
        ),
      ],
    );
  }
}

class _DataNote extends StatelessWidget {
  const _DataNote({required this.text});

  final String text;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: const Color(0xFFFFF7E6),
        borderRadius: BorderRadius.circular(14),
      ),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Icon(
            Icons.info_outline_rounded,
            color: Color(0xFFAA6800),
            size: 20,
          ),
          const SizedBox(width: 9),
          Expanded(
            child: Text(
              text,
              style: const TextStyle(color: Color(0xFF7C5309), height: 1.4),
            ),
          ),
        ],
      ),
    );
  }
}

class _EmptyExams extends StatelessWidget {
  const _EmptyExams();

  @override
  Widget build(BuildContext context) => const Card(
    child: Padding(
      padding: EdgeInsets.all(22),
      child: Column(
        children: [
          Icon(
            Icons.library_books_outlined,
            size: 36,
            color: Color(0xFF8190A6),
          ),
          SizedBox(height: 8),
          Text('Chưa có đề thi được xuất bản.'),
        ],
      ),
    ),
  );
}
