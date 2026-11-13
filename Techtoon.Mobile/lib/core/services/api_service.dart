import 'dart:async';
import 'dart:convert';
import 'dart:io';

import 'package:http/http.dart' as http;

/// Thrown when the API call fails. [message] is safe to show to the user.
class ApiException implements Exception {
  final String message;
  final int? statusCode;
  final bool isNetworkError;

  ApiException(this.message, {this.statusCode, this.isNetworkError = false});

  @override
  String toString() => message;
}

class ApiService {
  /// Base URL is configurable at build time:
  /// `flutter build web --dart-define=API_BASE_URL=https://...`
  static String get baseUrl => const String.fromEnvironment(
        'API_BASE_URL',
        defaultValue: 'https://techtoon-api.fly.dev',
      );

  String? _token;
  String? _refreshToken;

  String? get token => _token;
  String? get refreshToken => _refreshToken;
  bool get isLoggedIn => _token != null;

  void setToken(String token) {
    _token = token.isEmpty ? null : token;
    if (token.isEmpty) _refreshToken = null;
  }

  void setTokens({String? accessToken, String? refreshToken}) {
    if (accessToken != null) _token = accessToken.isEmpty ? null : accessToken;
    if (refreshToken != null) _refreshToken = refreshToken;
  }

  void clearSession() {
    _token = null;
    _refreshToken = null;
  }

  Map<String, String> get _headers => {
        'Content-Type': 'application/json',
        'Accept': 'application/json',
        if (_token != null) 'Authorization': 'Bearer $_token',
      };

  Future<Map<String, dynamic>> login(String username, String password) async {
    final response = await _send(
      () => http.post(
        Uri.parse('$baseUrl/api/auth/login'),
        headers: {'Content-Type': 'application/json'},
        body: jsonEncode({
          'username': username,
          'password': password,
          'deviceId': 'flutter-app',
          'deviceName': 'Flutter Mobile',
        }),
      ),
    );

    if (response.statusCode == 200) {
      final data = jsonDecode(response.body) as Map<String, dynamic>;
      setTokens(
        accessToken: data['accessToken'] as String?,
        refreshToken: data['refreshToken'] as String?,
      );
      return data;
    }
    // A 401 on the login endpoint means bad credentials, not an expired session.
    if (response.statusCode == 401) {
      throw ApiException('اسم المستخدم أو كلمة المرور غير صحيحة',
          statusCode: 401);
    }
    throw _errorFrom(response, fallback: 'اسم المستخدم أو كلمة المرور غير صحيحة');
  }

  /// Exchanges the stored refresh token for a new access token.
  /// Returns true on success. Clears the session on failure.
  Future<bool> refreshAccessToken() async {
    final refresh = _refreshToken;
    if (refresh == null || refresh.isEmpty) return false;

    try {
      final response = await _send(
        () => http.post(
          Uri.parse('$baseUrl/api/auth/refresh'),
          headers: {'Content-Type': 'application/json'},
          body: jsonEncode({'refreshToken': refresh, 'deviceId': 'flutter-app'}),
        ),
      );

      if (response.statusCode == 200) {
        final data = jsonDecode(response.body) as Map<String, dynamic>;
        setTokens(
          accessToken: data['accessToken'] as String?,
          refreshToken: data['refreshToken'] as String? ?? refresh,
        );
        return true;
      }
    } catch (_) {
      // fall through to clearing the session
    }

    clearSession();
    return false;
  }

  /// Executes [request]; on HTTP 401 refreshes the token once and retries.
  Future<http.Response> _authed(Future<http.Response> Function() request) async {
    var response = await _send(request);
    if (response.statusCode == 401 && _refreshToken != null) {
      final refreshed = await refreshAccessToken();
      if (refreshed) {
        response = await _send(request);
      }
    }
    if (response.statusCode == 401) {
      clearSession();
      throw ApiException('انتهت الجلسة، يرجى تسجيل الدخول مرة أخرى', statusCode: 401);
    }
    return response;
  }

  Future<http.Response> _send(Future<http.Response> Function() request) async {
    try {
      return await request().timeout(const Duration(seconds: 30));
    } on SocketException {
      throw ApiException('تعذر الاتصال بالخادم، تحقق من اتصال الإنترنت',
          isNetworkError: true);
    } on TimeoutException {
      throw ApiException('انتهت مهلة الاتصال بالخادم', isNetworkError: true);
    } on http.ClientException {
      throw ApiException('تعذر الاتصال بالخادم، تحقق من اتصال الإنترنت',
          isNetworkError: true);
    }
  }

  ApiException _errorFrom(http.Response response, {String? fallback}) {
    String message = fallback ?? 'حدث خطأ غير متوقع';
    try {
      final body = jsonDecode(response.body);
      if (body is Map) {
        message = (body['message'] ?? body['title'] ?? body['detail'] ?? message)
            .toString();
      }
    } catch (_) {
      // non-JSON error body — keep the fallback
    }
    switch (response.statusCode) {
      case 400:
        message = message.isEmpty ? 'بيانات غير صحيحة' : message;
        break;
      case 401:
        message = 'انتهت الجلسة، يرجى تسجيل الدخول مرة أخرى';
        break;
      case 403:
        message = 'ليس لديك صلاحية لهذا الإجراء';
        break;
      case 404:
        message = 'العنصر المطلوب غير موجود';
        break;
      case 500:
      case 502:
      case 503:
        message = 'خطأ في الخادم، يرجى المحاولة لاحقاً';
        break;
    }
    return ApiException(message, statusCode: response.statusCode);
  }

  Future<List<dynamic>> getList(String endpoint) async {
    final response = await _authed(
      () => http.get(Uri.parse('$baseUrl/$endpoint'), headers: _headers),
    );
    if (response.statusCode == 200) {
      final data = jsonDecode(response.body);
      if (data is Map) return data['items'] ?? [];
      return data ?? [];
    }
    throw _errorFrom(response);
  }

  /// Calls the API health endpoint (`GET /health`) and returns the raw body.
  /// Throws [ApiException] when the server is unreachable or answers with an error.
  Future<String> checkHealth() async {
    final response = await _send(
      () => http.get(Uri.parse('$baseUrl/health')),
    );
    if (response.statusCode >= 200 && response.statusCode < 300) {
      return response.body;
    }
    throw _errorFrom(response, fallback: 'الخادم لا يستجيب بشكل صحيح');
  }

  /// Fetches every item of a paged endpoint (`items`/`totalCount`/`totalPages`),
  /// following the pagination until all pages are read.
  Future<List<dynamic>> getAllItems(String endpoint, {int pageSize = 200}) async {
    final all = <dynamic>[];
    var page = 1;
    var totalPages = 1;
    while (page <= totalPages && page <= 200) {
      final separator = endpoint.contains('?') ? '&' : '?';
      final data = await getItem(
        '$endpoint${separator}page=$page&pageSize=$pageSize',
      );
      final items = data['items'];
      if (items is List) all.addAll(items);
      final pages = data['totalPages'];
      final pageCount = (pages is int) ? pages as int : 1;
      totalPages = pageCount > 0 ? pageCount : 1;
      page++;
    }
    return all;
  }

  /// Fetches every item of [endpoint] and keeps only those whose [field]
  /// equals [value].
  ///
  /// The API currently answers 400 ("The parameter already belongs to a
  /// collection") for every request that carries a server-side `type` filter,
  /// so the filtering is done on the device instead of on the server.
  Future<List<dynamic>> getItemsByField(
      String endpoint, String field, String value) async {
    final all = await getAllItems(endpoint);
    return all
        .where((item) => item is Map && item[field]?.toString() == value)
        .toList();
  }

  Future<Map<String, dynamic>> getItem(String endpoint) async {
    final response = await _authed(
      () => http.get(Uri.parse('$baseUrl/$endpoint'), headers: _headers),
    );
    if (response.statusCode == 200) {
      return jsonDecode(response.body) as Map<String, dynamic>;
    }
    throw _errorFrom(response);
  }

  Future<String> getRaw(String endpoint) async {
    final response = await _authed(
      () => http.get(Uri.parse('$baseUrl/$endpoint'), headers: _headers),
    );
    if (response.statusCode == 200) return response.body;
    throw _errorFrom(response);
  }

  Future<dynamic> decodeData(String rawData) async => jsonDecode(rawData);
  Future<String> getData(String endpoint) async => getRaw(endpoint);

  Future<Map<String, dynamic>?> post(
      String endpoint, Map<String, dynamic> body) async {
    final response = await _authed(
      () => http.post(
        Uri.parse('$baseUrl/$endpoint'),
        headers: _headers,
        body: jsonEncode(body),
      ),
    );
    if (response.statusCode == 200 || response.statusCode == 201) {
      if (response.body.isEmpty) return null;
      return jsonDecode(response.body) as Map<String, dynamic>;
    }
    throw _errorFrom(response);
  }

  Future<Map<String, dynamic>?> put(
      String endpoint, Map<String, dynamic> body) async {
    final response = await _authed(
      () => http.put(
        Uri.parse('$baseUrl/$endpoint'),
        headers: _headers,
        body: jsonEncode(body),
      ),
    );
    if (response.statusCode == 200) {
      if (response.body.isEmpty) return null;
      return jsonDecode(response.body) as Map<String, dynamic>;
    }
    throw _errorFrom(response);
  }

  Future<bool> delete(String endpoint) async {
    final response = await _authed(
      () => http.delete(Uri.parse('$baseUrl/$endpoint'), headers: _headers),
    );
    if (response.statusCode == 204 || response.statusCode == 200) return true;
    throw _errorFrom(response);
  }
}
