// Tệp này định nghĩa các kiểu dữ liệu dùng chung cho giao diện, gồm người dùng, danh mục và toàn bộ mô hình Recipe.
// Chức năng: cung cấp type/interface cho công thức, ảnh, nguyên liệu, bước làm, dinh dưỡng, xác thực và thông báo.

export type DifficultyLevel = "Easy" | "Medium" | "Hard";
export type RecipeStatus = "Draft" | "Published" | "Archived";

export interface User {
  id: string;
  displayName: string;
  userName: string;
  email: string;
  avatarUrl: string;
  bio?: string;
  role: "User" | "Admin";
  createdAt: string;
}

export interface Category {
  id: string;
  name: string;
  slug: string;
  description: string;
  icon: string;
  recipeCount: number;
}

export interface RecipeImage {
  id: string;
  url: string;
  isPrimary: boolean;
  caption?: string;
  rowVersion?: string;
  file?: File;
}

export interface Ingredient {
  id: string;
  amount: string;
  unit: string;
  name: string;
  rowVersion?: string;
}

export interface RecipeStep {
  id: string;
  stepNumber: number;
  title?: string;
  description: string;
  timerMinutes?: number;
  imageUrl?: string;
  rowVersion?: string;
}

export interface NutritionInfo {
  calories?: number;
  protein?: number;
  carbs?: number;
  fat?: number;
}

export interface Recipe {
  id: string;
  title: string;
  slug: string;
  description: string;
  prepTime: number; // in minutes
  cookTime: number; // in minutes
  servings: number;
  difficultyLevel: DifficultyLevel;
  status: RecipeStatus;
  images: RecipeImage[];
  ingredients: Ingredient[];
  steps: RecipeStep[];
  nutrition?: NutritionInfo;
  author: User;
  category: Category;
  createdAt: string;
  updatedAt: string;
  viewsCount: number;
  likesCount: number;
  rowVersion?: string;
  instructions?: string;
}

export type ToastType = "success" | "error" | "warning" | "info";

export interface ToastMessage {
  id: string;
  type: ToastType;
  title?: string;
  message: string;
  duration?: number;
}

export interface AuthUser {
  id: string;
  displayName: string;
  userName: string;
  email: string;
  avatarUrl?: string;
  bio?: string;
  roles: string[];  // e.g. ["Author"]
  role?: "Author" | "Admin" | "User";
  createdAt?: string;
}

export interface RegisterRequest {
  email: string;
  userName: string;
  displayName: string;
  password: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  user: AuthUser;
}

export interface ApiValidationErrorDetail {
  field: string;
  message: string;
}

export interface ApiError {
  errorCode?: string;  // e.g. "AUTH_EMAIL_EXISTS", "AUTH_USERNAME_EXISTS"
  error?: string;
  statusCode?: number;
  message?: string;
  errors?: Array<{ field: string; message: string }>;
}
