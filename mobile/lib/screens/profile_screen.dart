import 'package:flutter/material.dart';

import '../models/student_profile.dart';

class ProfileScreen extends StatelessWidget {
  const ProfileScreen({
    required this.profile,
    required this.onSignOut,
    super.key,
  });

  final StudentProfile profile;
  final Future<void> Function() onSignOut;

  Future<void> _confirmSignOut(BuildContext context) async {
    final shouldSignOut = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Đăng xuất khỏi ứng dụng?'),
        content: const Text('Phiên đăng nhập trên thiết bị sẽ được xóa.'),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(context).pop(false),
            child: const Text('Ở lại'),
          ),
          FilledButton(
            onPressed: () => Navigator.of(context).pop(true),
            child: const Text('Đăng xuất'),
          ),
        ],
      ),
    );
    if (shouldSignOut == true) await onSignOut();
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return SafeArea(
      child: ListView(
        padding: const EdgeInsets.fromLTRB(20, 16, 20, 24),
        children: [
          Text(
            'Hồ sơ sinh viên',
            style: theme.textTheme.headlineSmall?.copyWith(
              fontWeight: FontWeight.w800,
            ),
          ),
          const SizedBox(height: 20),
          Card(
            child: Padding(
              padding: const EdgeInsets.all(20),
              child: Column(
                children: [
                  const CircleAvatar(
                    radius: 38,
                    backgroundColor: Color(0xFFE6EEFF),
                    child: Icon(
                      Icons.person_rounded,
                      size: 39,
                      color: Color(0xFF2864E8),
                    ),
                  ),
                  const SizedBox(height: 12),
                  Text(
                    profile.displayName,
                    textAlign: TextAlign.center,
                    style: theme.textTheme.titleLarge?.copyWith(
                      fontWeight: FontWeight.w800,
                    ),
                  ),
                  const SizedBox(height: 4),
                  Text(
                    '@${profile.username}',
                    style: const TextStyle(color: Color(0xFF68758A)),
                  ),
                  const SizedBox(height: 18),
                  const Divider(height: 1),
                  const SizedBox(height: 8),
                  _ProfileRow(
                    icon: Icons.alternate_email_rounded,
                    label: 'Email',
                    value: profile.emailAddress.isEmpty
                        ? 'Chưa có email'
                        : profile.emailAddress,
                  ),
                  _ProfileRow(
                    icon: Icons.verified_user_outlined,
                    label: 'Vai trò',
                    value: profile.roleNames.join(', '),
                  ),
                  _ProfileRow(
                    icon: Icons.badge_outlined,
                    label: 'Mã tài khoản',
                    value: '${profile.id}',
                  ),
                ],
              ),
            ),
          ),
          const SizedBox(height: 18),
          OutlinedButton.icon(
            onPressed: () => _confirmSignOut(context),
            icon: const Icon(Icons.logout_rounded),
            label: const Text('Đăng xuất'),
            style: OutlinedButton.styleFrom(
              foregroundColor: const Color(0xFFB42318),
              minimumSize: const Size.fromHeight(52),
              side: const BorderSide(color: Color(0xFFF1C8C4)),
            ),
          ),
        ],
      ),
    );
  }
}

class _ProfileRow extends StatelessWidget {
  const _ProfileRow({
    required this.icon,
    required this.label,
    required this.value,
  });

  final IconData icon;
  final String label;
  final String value;

  @override
  Widget build(BuildContext context) => Padding(
    padding: const EdgeInsets.symmetric(vertical: 11),
    child: Row(
      children: [
        Icon(icon, size: 20, color: const Color(0xFF68758A)),
        const SizedBox(width: 11),
        SizedBox(
          width: 96,
          child: Text(label, style: const TextStyle(color: Color(0xFF68758A))),
        ),
        Expanded(
          child: Text(
            value,
            textAlign: TextAlign.end,
            style: const TextStyle(
              color: Color(0xFF25334B),
              fontWeight: FontWeight.w600,
            ),
          ),
        ),
      ],
    ),
  );
}
