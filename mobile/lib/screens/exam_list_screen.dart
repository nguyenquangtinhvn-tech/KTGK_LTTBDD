import 'package:flutter/material.dart';

import '../models/exam.dart';
import '../repositories/student_repository.dart';
import '../services/api_client.dart';
import 'exam_detail_screen.dart';

class ExamListScreen extends StatefulWidget {
  const ExamListScreen({required this.repository, super.key});

  final StudentRepository repository;

  @override
  State<ExamListScreen> createState() => _ExamListScreenState();
}

class _ExamListScreenState extends State<ExamListScreen> {
  final _searchController = TextEditingController();
  late Future<List<Exam>> _examsFuture;
  String _query = '';

  @override
  void initState() {
    super.initState();
    _examsFuture = widget.repository.getPublishedExams();
  }

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  Future<void> _reload() async {
    final future = widget.repository.getPublishedExams();
    setState(() => _examsFuture = future);
    await future.then<void>((_) {}, onError: (Object _) {});
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
          keyboardDismissBehavior: ScrollViewKeyboardDismissBehavior.onDrag,
          physics: const AlwaysScrollableScrollPhysics(),
          slivers: [
            SliverPadding(
              padding: const EdgeInsets.fromLTRB(20, 16, 20, 20),
              sliver: SliverList(
                delegate: SliverChildListDelegate([
                  Text(
                    'Thư viện đề thi',
                    style: theme.textTheme.headlineSmall?.copyWith(
                      fontWeight: FontWeight.w800,
                    ),
                  ),
                  const SizedBox(height: 6),
                  Text(
                    'Tìm đề theo tên hoặc mã đề.',
                    style: theme.textTheme.bodyMedium?.copyWith(
                      color: const Color(0xFF68758A),
                    ),
                  ),
                  const SizedBox(height: 18),
                  TextField(
                    controller: _searchController,
                    onChanged: (value) => setState(() => _query = value.trim()),
                    decoration: InputDecoration(
                      hintText: 'Tìm tên đề hoặc mã đề',
                      prefixIcon: const Icon(Icons.search_rounded),
                      suffixIcon: _query.isEmpty
                          ? null
                          : IconButton(
                              tooltip: 'Xóa tìm kiếm',
                              onPressed: () {
                                _searchController.clear();
                                setState(() => _query = '');
                              },
                              icon: const Icon(Icons.close_rounded),
                            ),
                    ),
                  ),
                  const SizedBox(height: 12),
                  Container(
                    padding: const EdgeInsets.all(12),
                    decoration: BoxDecoration(
                      color: const Color(0xFFEAF1FF),
                      borderRadius: BorderRadius.circular(14),
                    ),
                    child: const Row(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Icon(
                          Icons.filter_alt_outlined,
                          size: 19,
                          color: Color(0xFF2864E8),
                        ),
                        SizedBox(width: 8),
                        Expanded(
                          child: Text(
                            'Danh sách chỉ gồm đề đã xuất bản theo API hiện có.',
                            style: TextStyle(
                              color: Color(0xFF31558F),
                              height: 1.35,
                            ),
                          ),
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(height: 20),
                  FutureBuilder<List<Exam>>(
                    future: _examsFuture,
                    builder: (context, snapshot) {
                      if (snapshot.hasError) {
                        return _ExamListError(
                          error: snapshot.error!,
                          onRetry: _reload,
                        );
                      }
                      if (snapshot.connectionState != ConnectionState.done) {
                        return const Padding(
                          padding: EdgeInsets.symmetric(vertical: 40),
                          child: Center(child: CircularProgressIndicator()),
                        );
                      }

                      final exams = snapshot.data!;
                      final normalizedQuery = _query.toLowerCase();
                      final visibleExams = exams
                          .where((exam) {
                            return exam.title.toLowerCase().contains(
                                  normalizedQuery,
                                ) ||
                                exam.id.toString().contains(normalizedQuery);
                          })
                          .toList(growable: false);

                      if (visibleExams.isEmpty) {
                        return _ExamListEmpty(hasSearch: _query.isNotEmpty);
                      }

                      return Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            '${visibleExams.length} đề thi',
                            style: theme.textTheme.titleSmall?.copyWith(
                              color: const Color(0xFF68758A),
                              fontWeight: FontWeight.w600,
                            ),
                          ),
                          const SizedBox(height: 10),
                          ...visibleExams.map(
                            (exam) => Padding(
                              padding: const EdgeInsets.only(bottom: 12),
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

class ExamCard extends StatelessWidget {
  const ExamCard({required this.exam, required this.onTap, super.key});

  final Exam exam;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Card(
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(20),
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Container(
                    width: 44,
                    height: 44,
                    decoration: BoxDecoration(
                      color: const Color(0xFFEAF1FF),
                      borderRadius: BorderRadius.circular(14),
                    ),
                    child: const Icon(
                      Icons.description_outlined,
                      color: Color(0xFF2864E8),
                    ),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          exam.title,
                          maxLines: 2,
                          overflow: TextOverflow.ellipsis,
                          style: theme.textTheme.titleMedium?.copyWith(
                            fontWeight: FontWeight.w700,
                          ),
                        ),
                        const SizedBox(height: 4),
                        Text(
                          'Mã đề #${exam.id}',
                          style: theme.textTheme.bodySmall?.copyWith(
                            color: const Color(0xFF68758A),
                          ),
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(width: 6),
                  const _PublishedBadge(),
                ],
              ),
              if (exam.description != null &&
                  exam.description!.trim().isNotEmpty) ...[
                const SizedBox(height: 12),
                Text(
                  exam.description!,
                  maxLines: 2,
                  overflow: TextOverflow.ellipsis,
                  style: theme.textTheme.bodyMedium?.copyWith(
                    color: const Color(0xFF5F6D82),
                    height: 1.4,
                  ),
                ),
              ],
              const SizedBox(height: 15),
              Row(
                children: [
                  const Icon(
                    Icons.quiz_outlined,
                    size: 17,
                    color: Color(0xFF68758A),
                  ),
                  const SizedBox(width: 5),
                  Text('${exam.questionCount} câu'),
                  const SizedBox(width: 16),
                  const Icon(
                    Icons.schedule_rounded,
                    size: 17,
                    color: Color(0xFF68758A),
                  ),
                  const SizedBox(width: 5),
                  Text('${exam.durationMinutes} phút'),
                  const Spacer(),
                  const Icon(
                    Icons.arrow_forward_rounded,
                    color: Color(0xFF2864E8),
                    size: 20,
                  ),
                ],
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _PublishedBadge extends StatelessWidget {
  const _PublishedBadge();

  @override
  Widget build(BuildContext context) => Container(
    padding: const EdgeInsets.symmetric(horizontal: 9, vertical: 6),
    decoration: BoxDecoration(
      color: const Color(0xFFE7F7F1),
      borderRadius: BorderRadius.circular(20),
    ),
    child: const Text(
      'Đã xuất bản',
      style: TextStyle(
        color: Color(0xFF13845F),
        fontSize: 11,
        fontWeight: FontWeight.w700,
      ),
    ),
  );
}

class _ExamListError extends StatelessWidget {
  const _ExamListError({required this.error, required this.onRetry});

  final Object error;
  final Future<void> Function() onRetry;

  @override
  Widget build(BuildContext context) => Column(
    children: [
      const Icon(Icons.cloud_off_rounded, size: 40, color: Color(0xFF8794A8)),
      const SizedBox(height: 12),
      Text(errorMessage(error), textAlign: TextAlign.center),
      const SizedBox(height: 8),
      OutlinedButton.icon(
        onPressed: onRetry,
        icon: const Icon(Icons.refresh_rounded),
        label: const Text('Thử lại'),
      ),
    ],
  );
}

class _ExamListEmpty extends StatelessWidget {
  const _ExamListEmpty({required this.hasSearch});

  final bool hasSearch;

  @override
  Widget build(BuildContext context) => Card(
    child: Padding(
      padding: const EdgeInsets.all(24),
      child: Column(
        children: [
          const Icon(
            Icons.search_off_rounded,
            size: 38,
            color: Color(0xFF8794A8),
          ),
          const SizedBox(height: 10),
          Text(
            hasSearch
                ? 'Không tìm thấy đề phù hợp.'
                : 'Chưa có đề thi được xuất bản.',
            textAlign: TextAlign.center,
          ),
        ],
      ),
    ),
  );
}
