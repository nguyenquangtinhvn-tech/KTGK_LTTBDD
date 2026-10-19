import 'dart:async';

import 'package:flutter/material.dart';

import '../models/exam.dart';
import '../models/student_attempt.dart';
import '../repositories/student_repository.dart';
import '../services/api_client.dart';
import 'exam_result_screen.dart';

class ExamAttemptScreen extends StatefulWidget {
  const ExamAttemptScreen({
    required this.attempt,
    required this.exam,
    required this.repository,
    super.key,
  });

  final StudentAttempt attempt;
  final Exam exam;
  final StudentRepository repository;

  @override
  State<ExamAttemptScreen> createState() => _ExamAttemptScreenState();
}

class _ExamAttemptScreenState extends State<ExamAttemptScreen> {
  late final Map<int, AttemptPassage> _passagesById;
  late final Map<int, int> _selectedOptions;
  late final DateTime _localExpiry;
  late int _currentIndex;
  late int _remainingSeconds;
  late final Timer _timer;
  final Set<int> _markedForReview = {};
  final Set<int> _unsavedQuestionIds = {};
  Future<void> _saveQueue = Future<void>.value();
  Object? _saveError;
  bool _isSubmitting = false;
  int _savingCount = 0;
  bool _resultOpened = false;
  bool _autoSubmitFailed = false;

  @override
  void initState() {
    super.initState();
    _passagesById = {
      for (final passage in widget.attempt.readingPassages) passage.id: passage,
    };
    _selectedOptions = Map<int, int>.from(widget.attempt.answers);
    _currentIndex = 0;
    _remainingSeconds = widget.attempt.remainingSeconds;
    _localExpiry = DateTime.now().add(
      Duration(seconds: widget.attempt.remainingSeconds),
    );
    _timer = Timer.periodic(const Duration(seconds: 1), (_) {
      final remaining = _localExpiry.difference(DateTime.now()).inSeconds;
      if (!mounted) return;
      setState(() => _remainingSeconds = remaining > 0 ? remaining : 0);
      if (remaining <= 0 && !_autoSubmitFailed) _submit(expired: true);
    });
    if (widget.attempt.isSubmitted && widget.attempt.result != null) {
      WidgetsBinding.instance.addPostFrameCallback((_) {
        _openResult(widget.attempt.result!);
      });
    }
  }

  @override
  void dispose() {
    _timer.cancel();
    super.dispose();
  }

  int get _answeredCount => _selectedOptions.length;

  Future<void> _saveSelection(AttemptQuestion question, int? optionId) async {
    setState(() {
      if (optionId == null) {
        _selectedOptions.remove(question.id);
      } else {
        _selectedOptions[question.id] = optionId;
      }
      _unsavedQuestionIds.add(question.id);
      _savingCount++;
      _saveError = null;
    });

    final previousSave = _saveQueue;
    final nextSave = Completer<void>();
    _saveQueue = nextSave.future;
    try {
      await previousSave;
      final updated = await widget.repository.saveAnswer(
        attemptId: widget.attempt.attemptId,
        questionId: question.id,
        selectedOptionId: optionId,
      );
      if (mounted && !updated.isSubmitted) {
        setState(() {
          if (_selectedOptions[question.id] == optionId) {
            _unsavedQuestionIds.remove(question.id);
          }
        });
      }
      if (updated.isSubmitted && updated.result != null) {
        _openResult(
          updated.result!,
          warningMessage: _unsavedQuestionIds.isEmpty
              ? null
              : 'Một số lựa chọn chưa được máy chủ xác nhận trước khi hết giờ.',
        );
      }
    } catch (error) {
      if (mounted) {
        setState(() => _saveError = error);
      }
    } finally {
      if (mounted) setState(() => _savingCount--);
      nextSave.complete();
    }
  }

  Future<void> _retryPendingSaves() async {
    for (final questionId in _unsavedQuestionIds.toList()) {
      final question = widget.attempt.questions.firstWhere(
        (item) => item.id == questionId,
      );
      await _saveSelection(question, _selectedOptions[questionId]);
    }
  }

  Future<void> _submit({bool expired = false, bool retry = false}) async {
    if (_isSubmitting || _resultOpened) return;
    if (expired && _autoSubmitFailed && !retry) return;
    if (!expired) {
      final shouldSubmit = await showDialog<bool>(
        context: context,
        builder: (context) => AlertDialog(
          title: const Text('Nộp bài thi?'),
          content: Text(
            'Bạn đã trả lời $_answeredCount/${widget.attempt.questions.length} '
            'câu. Sau khi nộp sẽ không thể thay đổi câu trả lời.',
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(context, false),
              child: const Text('Tiếp tục làm bài'),
            ),
            FilledButton(
              onPressed: () => Navigator.pop(context, true),
              child: const Text('Nộp bài'),
            ),
          ],
        ),
      );
      if (shouldSubmit != true) return;
    }
    if (!mounted) return;

    setState(() {
      _isSubmitting = true;
      _saveError = null;
    });
    try {
      await _saveQueue;
      if (!expired && _unsavedQuestionIds.isNotEmpty) {
        throw const ApiException(
          message:
              'Có câu trả lời chưa được máy chủ lưu. Hãy lưu lại trước khi nộp bài.',
        );
      }
      final result = await widget.repository.submitAttempt(
        widget.attempt.attemptId,
      );
      _openResult(
        result,
        warningMessage: _unsavedQuestionIds.isEmpty
            ? null
            : 'Một số lựa chọn chưa được máy chủ xác nhận trước khi hết giờ.',
      );
    } catch (error) {
      if (!mounted) return;
      if (expired) _autoSubmitFailed = true;
      setState(() => _saveError = error);
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(
            expired
                ? 'Đã hết giờ nhưng chưa thể nộp bài. Hãy thử nộp lại khi có mạng.'
                : errorMessage(error),
          ),
          action: SnackBarAction(
            label: 'Thử lại',
            onPressed: () => _submit(expired: true, retry: true),
          ),
        ),
      );
    } finally {
      if (mounted) setState(() => _isSubmitting = false);
    }
  }

  void _openResult(StudentExamResult result, {String? warningMessage}) {
    if (!mounted || _resultOpened) return;
    _resultOpened = true;
    _timer.cancel();
    Navigator.of(context).pushReplacement(
      MaterialPageRoute<void>(
        builder: (_) => ExamResultScreen(
          result: result,
          repository: widget.repository,
          exam: widget.exam,
          warningMessage: warningMessage,
        ),
      ),
    );
  }

  Future<void> _showQuestionNavigator() async {
    final index = await showModalBottomSheet<int>(
      context: context,
      isScrollControlled: true,
      builder: (context) => SafeArea(
        child: Padding(
          padding: const EdgeInsets.all(20),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                'Điều hướng câu hỏi',
                style: Theme.of(context).textTheme.titleLarge,
              ),
              const SizedBox(height: 16),
              Wrap(
                spacing: 9,
                runSpacing: 9,
                children: [
                  for (var index = 0;
                      index < widget.attempt.questions.length;
                      index++)
                    _QuestionNumberButton(
                      number: widget.attempt.questions[index].questionNumber,
                      selected: index == _currentIndex,
                      answered: _selectedOptions.containsKey(
                        widget.attempt.questions[index].id,
                      ),
                      marked: _markedForReview.contains(
                        widget.attempt.questions[index].id,
                      ),
                      onTap: () => Navigator.pop(context, index),
                    ),
                ],
              ),
            ],
          ),
        ),
      ),
    );
    if (index != null && mounted) setState(() => _currentIndex = index);
  }

  @override
  Widget build(BuildContext context) {
    final questions = widget.attempt.questions;
    if (questions.isEmpty) {
      return Scaffold(
        appBar: AppBar(title: const Text('Làm bài thi')),
        body: const Center(child: Text('Đề thi không có câu hỏi.')),
      );
    }

    final question = questions[_currentIndex];
    final passage = question.readingPassageId == null
        ? null
        : _passagesById[question.readingPassageId];
    final selectedOptionId = _selectedOptions[question.id];
    final markedForReview = _markedForReview.contains(question.id);

    return Scaffold(
      appBar: AppBar(
        title: Text('Câu ${question.questionNumber} / ${questions.length}'),
        actions: [
          IconButton(
            tooltip: 'Điều hướng câu hỏi',
            onPressed: _showQuestionNavigator,
            icon: const Icon(Icons.grid_view_rounded),
          ),
        ],
      ),
      body: SafeArea(
        child: Column(
          children: [
            Padding(
              padding: const EdgeInsets.fromLTRB(20, 4, 20, 10),
              child: Row(
                children: [
                  Expanded(
                    child: LinearProgressIndicator(
                      value: (_currentIndex + 1) / questions.length,
                      minHeight: 7,
                      borderRadius: BorderRadius.circular(8),
                    ),
                  ),
                  const SizedBox(width: 12),
                  _TimerLabel(seconds: _remainingSeconds),
                ],
              ),
            ),
            Expanded(
              child: ListView(
                padding: const EdgeInsets.fromLTRB(20, 8, 20, 16),
                children: [
                  if (passage != null) ...[
                    Card(
                      child: Padding(
                        padding: const EdgeInsets.all(16),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(
                              passage.title.isEmpty
                                  ? 'Bài đọc · Part ${passage.partNumber}'
                                  : passage.title,
                              style: const TextStyle(
                                fontWeight: FontWeight.w700,
                              ),
                            ),
                            const SizedBox(height: 10),
                            Text(
                              passage.content,
                              style: const TextStyle(height: 1.5),
                            ),
                          ],
                        ),
                      ),
                    ),
                    const SizedBox(height: 14),
                  ],
                  Text(
                    'Part ${question.partNumber} · Câu ${question.questionNumber}',
                    style: const TextStyle(
                      color: Color(0xFF2864E8),
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                  if (question.questionText.trim().isNotEmpty) ...[
                    const SizedBox(height: 8),
                    Text(
                      question.questionText,
                      style: Theme.of(context).textTheme.titleLarge?.copyWith(
                        fontWeight: FontWeight.w700,
                        height: 1.35,
                      ),
                    ),
                  ],
                  const SizedBox(height: 16),
                  RadioGroup<int>(
                    groupValue: selectedOptionId,
                    onChanged: (value) {
                      if (!_isSubmitting) _saveSelection(question, value);
                    },
                    child: Column(
                      children: [
                        for (final option in question.options)
                          Card(
                            margin: const EdgeInsets.only(bottom: 10),
                            child: RadioListTile<int>(
                              value: option.id,
                              title: Text(
                                '${option.label}. ${option.content}',
                                style: const TextStyle(height: 1.35),
                              ),
                              contentPadding: const EdgeInsets.symmetric(
                                horizontal: 10,
                                vertical: 3,
                              ),
                            ),
                          ),
                      ],
                    ),
                  ),
                  if (_savingCount > 0)
                    const _SaveStatus(
                      icon: Icons.cloud_upload_outlined,
                      message: 'Đang lưu câu trả lời…',
                      color: Color(0xFF2864E8),
                    ),
                  if (_saveError != null)
                    _SaveError(
                      error: _saveError!,
                      onRetry: _retryPendingSaves,
                    ),
                  TextButton.icon(
                    onPressed: () => setState(() {
                      if (markedForReview) {
                        _markedForReview.remove(question.id);
                      } else {
                        _markedForReview.add(question.id);
                      }
                    }),
                    icon: Icon(
                      markedForReview
                          ? Icons.bookmark
                          : Icons.bookmark_border_rounded,
                    ),
                    label: Text(
                      markedForReview
                          ? 'Bỏ đánh dấu xem lại'
                          : 'Đánh dấu xem lại',
                    ),
                  ),
                ],
              ),
            ),
            _AttemptFooter(
              answeredCount: _answeredCount,
              questionCount: questions.length,
              currentIndex: _currentIndex,
              isSubmitting: _isSubmitting,
              onPrevious: _currentIndex == 0
                  ? null
                  : () => setState(() => _currentIndex--),
              onNext: _currentIndex == questions.length - 1
                  ? null
                  : () => setState(() => _currentIndex++),
              onSubmit: () => _submit(),
            ),
          ],
        ),
      ),
    );
  }
}

class _TimerLabel extends StatelessWidget {
  const _TimerLabel({required this.seconds});

  final int seconds;

  @override
  Widget build(BuildContext context) {
    final minutes = seconds ~/ 60;
    final remainingSeconds = seconds % 60;
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 11, vertical: 8),
      decoration: BoxDecoration(
        color: seconds < 300 ? const Color(0xFFFFE9E8) : const Color(0xFFEAF1FF),
        borderRadius: BorderRadius.circular(12),
      ),
      child: Text(
        '${minutes.toString().padLeft(2, '0')}:'
        '${remainingSeconds.toString().padLeft(2, '0')}',
        style: TextStyle(
          fontWeight: FontWeight.w800,
          color: seconds < 300 ? const Color(0xFFB42318) : const Color(0xFF2456C8),
        ),
      ),
    );
  }
}

class _QuestionNumberButton extends StatelessWidget {
  const _QuestionNumberButton({
    required this.number,
    required this.selected,
    required this.answered,
    required this.marked,
    required this.onTap,
  });

  final int number;
  final bool selected;
  final bool answered;
  final bool marked;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final color = selected
        ? const Color(0xFF2864E8)
        : answered
        ? const Color(0xFFE7F7F1)
        : const Color(0xFFF1F3F7);
    return InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(12),
      child: Container(
        width: 45,
        height: 45,
        decoration: BoxDecoration(
          color: color,
          borderRadius: BorderRadius.circular(12),
          border: marked
              ? Border.all(color: const Color(0xFFEF9F22), width: 2)
              : null,
        ),
        alignment: Alignment.center,
        child: Text(
          '$number',
          style: TextStyle(
            color: selected ? Colors.white : const Color(0xFF26344C),
            fontWeight: FontWeight.w700,
          ),
        ),
      ),
    );
  }
}

class _AttemptFooter extends StatelessWidget {
  const _AttemptFooter({
    required this.answeredCount,
    required this.questionCount,
    required this.currentIndex,
    required this.isSubmitting,
    required this.onPrevious,
    required this.onNext,
    required this.onSubmit,
  });

  final int answeredCount;
  final int questionCount;
  final int currentIndex;
  final bool isSubmitting;
  final VoidCallback? onPrevious;
  final VoidCallback? onNext;
  final VoidCallback onSubmit;

  @override
  Widget build(BuildContext context) {
    return Material(
      color: Colors.white,
      elevation: 8,
      child: SafeArea(
        top: false,
        child: Padding(
          padding: const EdgeInsets.fromLTRB(16, 12, 16, 10),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Row(
                children: [
                  Expanded(
                    child: Text(
                      'Đã trả lời $answeredCount/$questionCount câu',
                      style: const TextStyle(
                        color: Color(0xFF68758A),
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                  ),
                  TextButton(
                    onPressed: isSubmitting ? null : onSubmit,
                    child: const Text('Nộp bài'),
                  ),
                ],
              ),
              Row(
                children: [
                  Expanded(
                    child: OutlinedButton.icon(
                      onPressed: onPrevious,
                      icon: const Icon(Icons.chevron_left_rounded),
                      label: const Text('Câu trước'),
                    ),
                  ),
                  const SizedBox(width: 10),
                  Expanded(
                    child: FilledButton.icon(
                      onPressed: onNext,
                      iconAlignment: IconAlignment.end,
                      icon: const Icon(Icons.chevron_right_rounded),
                      label: const Text('Câu tiếp'),
                    ),
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

class _SaveStatus extends StatelessWidget {
  const _SaveStatus({
    required this.icon,
    required this.message,
    required this.color,
  });

  final IconData icon;
  final String message;
  final Color color;

  @override
  Widget build(BuildContext context) => Padding(
    padding: const EdgeInsets.only(bottom: 8),
    child: Row(
      children: [
        Icon(icon, size: 18, color: color),
        const SizedBox(width: 7),
        Text(message, style: TextStyle(color: color)),
      ],
    ),
  );
}

class _SaveError extends StatelessWidget {
  const _SaveError({required this.error, required this.onRetry});

  final Object error;
  final VoidCallback onRetry;

  @override
  Widget build(BuildContext context) => Card(
    color: const Color(0xFFFFF1F0),
    child: Padding(
      padding: const EdgeInsets.all(12),
      child: Row(
        children: [
          Expanded(child: Text(errorMessage(error))),
          TextButton(onPressed: onRetry, child: const Text('Lưu lại')),
        ],
      ),
    ),
  );
}
