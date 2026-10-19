import 'package:flutter/material.dart';

import '../models/student_profile.dart';
import '../repositories/student_repository.dart';
import 'exam_list_screen.dart';
import 'history_screen.dart';
import 'home_screen.dart';
import 'profile_screen.dart';

class StudentShell extends StatefulWidget {
  const StudentShell({
    required this.profile,
    required this.repository,
    required this.onSignOut,
    super.key,
  });

  final StudentProfile profile;
  final StudentRepository repository;
  final Future<void> Function() onSignOut;

  @override
  State<StudentShell> createState() => _StudentShellState();
}

class _StudentShellState extends State<StudentShell> {
  int _selectedIndex = 0;

  @override
  Widget build(BuildContext context) {
    final pages = [
      HomeScreen(
        profile: widget.profile,
        repository: widget.repository,
        onBrowseExams: () => setState(() => _selectedIndex = 1),
      ),
      ExamListScreen(repository: widget.repository),
      HistoryScreen(repository: widget.repository),
      ProfileScreen(profile: widget.profile, onSignOut: widget.onSignOut),
    ];

    return Scaffold(
      body: IndexedStack(index: _selectedIndex, children: pages),
      bottomNavigationBar: NavigationBar(
        selectedIndex: _selectedIndex,
        onDestinationSelected: (index) =>
            setState(() => _selectedIndex = index),
        destinations: const [
          NavigationDestination(
            icon: Icon(Icons.home_outlined),
            selectedIcon: Icon(Icons.home_rounded),
            label: 'Trang chủ',
          ),
          NavigationDestination(
            icon: Icon(Icons.menu_book_outlined),
            selectedIcon: Icon(Icons.menu_book_rounded),
            label: 'Đề thi',
          ),
          NavigationDestination(
            icon: Icon(Icons.history_rounded),
            label: 'Lịch sử',
          ),
          NavigationDestination(
            icon: Icon(Icons.person_outline_rounded),
            selectedIcon: Icon(Icons.person_rounded),
            label: 'Hồ sơ',
          ),
        ],
      ),
    );
  }
}
