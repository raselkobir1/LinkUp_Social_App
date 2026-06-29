export interface RegisterDto {
  firstName: string;
  lastName: string;
  email: string;
  userName: string;
  password: string;
  confirmPassword: string;
}

export interface LoginDto {
  email: string;
  password: string;
}

export interface UserDto {
  id: string;
  firstName: string;
  lastName: string;
  fullName: string;
  email: string;
  userName: string;
  profilePictureUrl?: string;
  isOnline: boolean;
}

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  user: UserDto;
}

export interface RefreshTokenDto {
  accessToken: string;
  refreshToken: string;
}

export interface ForgotPasswordDto { email: string; }
export interface ResetPasswordDto { email: string; token: string; newPassword: string; confirmPassword: string; }
export interface ChangePasswordDto { currentPassword: string; newPassword: string; confirmPassword: string; }
