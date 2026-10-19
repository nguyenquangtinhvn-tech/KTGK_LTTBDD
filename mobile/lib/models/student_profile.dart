class StudentProfile {
  const StudentProfile({
    required this.id,
    required this.name,
    required this.surname,
    required this.username,
    required this.emailAddress,
    required this.roleNames,
  });

  final int id;
  final String name;
  final String surname;
  final String username;
  final String emailAddress;
  final List<String> roleNames;

  String get displayName {
    final fullName = '$name $surname'.trim();
    return fullName.isEmpty ? username : fullName;
  }

  bool get isStudent =>
      roleNames.any((role) => role.toLowerCase() == 'student');

  factory StudentProfile.fromJson(Map<String, dynamic> json) {
    final rawRoles = json['roleNames'];
    if (rawRoles is! List) {
      throw const FormatException(
        'Backend không trả về danh sách vai trò tài khoản.',
      );
    }

    return StudentProfile(
      id: _readInt(json['id'], 'id'),
      name: _readString(json['name'], 'name'),
      surname: _readString(json['surname'], 'surname'),
      username: _readString(json['userName'], 'userName'),
      emailAddress: _readString(json['emailAddress'], 'emailAddress'),
      roleNames: rawRoles
          .map((role) {
            if (role is! String) {
              throw const FormatException('Vai trò tài khoản không hợp lệ.');
            }
            return role;
          })
          .toList(growable: false),
    );
  }
}

int _readInt(Object? value, String fieldName) {
  if (value is int) return value;
  if (value is num) return value.toInt();
  throw FormatException('Trường $fieldName không hợp lệ trong hồ sơ.');
}

String _readString(Object? value, String fieldName) {
  if (value == null) return '';
  if (value is String) return value;
  throw FormatException('Trường $fieldName không hợp lệ trong hồ sơ.');
}
