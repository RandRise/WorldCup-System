export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  token: string;
  expiration: string;
}

export interface RegisterRequest {
  name: string;
  email: string;
  password: string;
}

export interface CurrentUser {
  id: number;
  name: string;
  email: string | null;
  roles: string[];
}
