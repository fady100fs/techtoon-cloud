import 'dart:convert';
import 'package:http/http.dart' as http;

class ApiService {
  static const String baseUrl = 'https://techtoon-api.fly.dev';
  String? _token;

  String? get token => _token;
  bool get isLoggedIn => _token != null;

  void setToken(String token) {
    _token = token;
  }

  Future<Map<String, dynamic>> login(String username, String password) async {
    final response = await http.post(
      Uri.parse('$baseUrl/api/auth/login'),
      headers: {'Content-Type': 'application/json'},
      body: jsonEncode({
        'username': username,
        'password': password,
        'deviceId': 'flutter-app',
        'deviceName': 'Flutter Mobile',
      }),
    );
    if (response.statusCode == 200) {
      final data = jsonDecode(response.body);
      setToken(data['accessToken']);
      return data;
    } else {
      throw Exception('Failed to login');
    }
  }

  Map<String, String> get _headers => {
    'Content-Type': 'application/json',
    if (_token != null) 'Authorization': 'Bearer $_token',
  };

  Future<List<dynamic>> getList(String endpoint) async {
    final response = await http.get(Uri.parse('$baseUrl/$endpoint'), headers: _headers);
    if (response.statusCode == 200) {
      final data = jsonDecode(response.body);
      return data['items'] ?? data ?? [];
    }
    return [];
  }

  Future<Map<String, dynamic>> getItem(String endpoint) async {
    final response = await http.get(Uri.parse('$baseUrl/$endpoint'), headers: _headers);
    if (response.statusCode == 200) {
      return jsonDecode(response.body);
    }
    return {};
  }

  Future<String> getRaw(String endpoint) async {
    final response = await http.get(Uri.parse('$baseUrl/$endpoint'), headers: _headers);
    if (response.statusCode == 200) return response.body;
    return 'Error: ${response.statusCode}';
  }

  Future<dynamic> decodeData(String rawData) async => jsonDecode(rawData);
  Future<String> getData(String endpoint) async => getRaw(endpoint);

  Future<Map<String, dynamic>?> post(String endpoint, Map<String, dynamic> body) async {
    final response = await http.post(
      Uri.parse('$baseUrl/$endpoint'),
      headers: _headers,
      body: jsonEncode(body),
    );
    if (response.statusCode == 200 || response.statusCode == 201) {
      return jsonDecode(response.body);
    }
    return null;
  }

  Future<Map<String, dynamic>?> put(String endpoint, Map<String, dynamic> body) async {
    final response = await http.put(
      Uri.parse('$baseUrl/$endpoint'),
      headers: _headers,
      body: jsonEncode(body),
    );
    if (response.statusCode == 200) {
      return jsonDecode(response.body);
    }
    return null;
  }

  Future<bool> delete(String endpoint) async {
    final response = await http.delete(Uri.parse('$baseUrl/$endpoint'), headers: _headers);
    return response.statusCode == 204 || response.statusCode == 200;
  }
}