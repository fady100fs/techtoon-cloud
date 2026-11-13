/// Models for the login response of POST /api/auth/login.
///
/// The API returns:
/// ```json
/// {
///   "accessToken": "...",
///   "refreshToken": "...",
///   "expiresAt": "2026-11-13T01:22:55.6387676Z",
///   "user": {
///     "id": 1,
///     "username": "admin",
///     "fullName": "admin",
///     "role": "ADMIN",
///     "permissions": ["dashboard.view", "..."]
///   }
/// }
/// ```
class User {
  final int id;
  final String username;
  final String fullName;
  final String role;
  final List<String> permissions;

  User({
    required this.id,
    required this.username,
    required this.fullName,
    required this.role,
    required this.permissions,
  });

  factory User.fromJson(Map<String, dynamic> json) {
    final rawId = json['id'];
    final rawPermissions = json['permissions'];
    return User(
      id: rawId is int ? rawId : 0,
      username: json['username']?.toString() ?? '',
      fullName: json['fullName']?.toString() ?? '',
      role: json['role']?.toString() ?? '',
      permissions: rawPermissions is List
          ? rawPermissions.map<String>((p) => p.toString()).toList()
          : <String>[],
    );
  }

  Map<String, dynamic> toJson() => {
        'id': id,
        'username': username,
        'fullName': fullName,
        'role': role,
        'permissions': permissions,
      };
}

class TokenResponse {
  final String accessToken;
  final String refreshToken;
  final DateTime? expiresAt;
  final User user;

  TokenResponse({
    required this.accessToken,
    required this.refreshToken,
    required this.user,
    this.expiresAt,
  });

  factory TokenResponse.fromJson(Map<String, dynamic> json) {
    final rawUser = json['user'];
    final rawExpiresAt = json['expiresAt'];
    return TokenResponse(
      accessToken: json['accessToken']?.toString() ?? '',
      refreshToken: json['refreshToken']?.toString() ?? '',
      expiresAt: rawExpiresAt == null
          ? null
          : DateTime.tryParse(rawExpiresAt.toString()),
      user: User.fromJson(
        rawUser is Map ? Map<String, dynamic>.from(rawUser) : <String, dynamic>{},
      ),
    );
  }
}
