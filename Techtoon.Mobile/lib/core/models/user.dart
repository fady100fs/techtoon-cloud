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
    return User(
      id: json['id'] ?? 0,
      username: json['username'] ?? '',
      fullName: json['fullName'] ?? '',
      role: json['role'] ?? '',
      permissions: List<String>.from(json['permissions'] ?? []),
    );
  }
}

class TokenResponse {
  final String accessToken;
  final String refreshToken;
  final User user;

  TokenResponse({
    required this.accessToken,
    required this.refreshToken,
    required this.user,
  });

  factory TokenResponse.fromJson(Map<String, dynamic> json) {
    return TokenResponse(
      accessToken: json['accessToken'] ?? '',
      refreshToken: json['refreshToken'] ?? '',
      user: User.fromJson(json['user'] ?? {}),
    );
  }
}