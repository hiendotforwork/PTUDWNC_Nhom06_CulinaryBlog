// Tệp này kết nối frontend với API xác thực và toàn bộ API quản lý công thức.
// Chức năng: xử lý phản hồi (handleResponse), tạo header xác thực (authHeaders), ánh xạ dữ liệu (mapRecipe, toMutation),
// đăng ký (register), đọc công thức (getRecipes, getRecipe), CRUD công thức (createRecipe, updateRecipeApi, mutateRecipe, deleteRecipeApi),
// CRUD nguyên liệu (addIngredient, updateIngredientApi, deleteIngredientApi), CRUD bước làm (addStep, updateStepApi, deleteStepApi),
// và quản lý ảnh (uploadRecipeImage, deleteImageApi, setPrimaryImageApi).

import {
  ApiError,
  AuthResponse,
  DifficultyLevel,
  Ingredient,
  Recipe,
  RecipeImage,
  RecipeStatus,
  RecipeStep,
  RegisterRequest,
} from "./types";
import { getToken } from "./auth";

const API_BASE = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5058";

type ApiNutrition = { calories?: number; protein?: number; carbohydrates?: number; fat?: number };
type ApiIngredient = { id: string; name: string; quantity?: number; unit?: string; rowVersion?: string };
type ApiStep = { id: string; stepNumber: number; title?: string; description: string; timerMinutes?: number; imageUrl?: string; rowVersion?: string };
type ApiImage = { id: string; originalUrl: string; mediumUrl?: string; thumbnailUrl?: string; altText?: string; isPrimary: boolean; rowVersion?: string };
type ApiRecipe = {
  id: string; title: string; slug: string; description: string; instructions?: string;
  prepTime: number; cookTime: number; servings: number; difficulty: number; status: number;
  categoryId: string; categoryName: string; authorId: string; authorName: string;
  primaryImageUrl?: string; images?: ApiImage[]; ingredients?: ApiIngredient[]; steps?: ApiStep[];
  nutrition?: ApiNutrition; createdAt: string; updatedAt?: string; rowVersion?: string;
};
type RecipePage = { items: ApiRecipe[] };
export type RecipeMutationResponse = { id: string; slug: string; status: number; rowVersion: string };
export type IngredientResponse = ApiIngredient & { orderIndex: number; rowVersion: string };
export type StepResponse = ApiStep & { rowVersion: string };
export type ImageResponse = ApiImage & { orderIndex: number; rowVersion: string };

// Chức năng: kiểm tra phản hồi HTTP và chuyển JSON sang kiểu T.
// Input: res - phản hồi nhận từ API. Output: dữ liệu kiểu T hoặc lỗi ApiError.
async function handleResponse<T>(res: Response): Promise<T> {
  if (!res.ok) {
    const error: ApiError = await res.json().catch(() => ({
      statusCode: res.status,
      message: res.status === 500 ? "Lỗi server, thử lại sau." : "Lỗi không xác định từ máy chủ.",
    }));
    if (res.status === 500 && !error.message) error.message = "Lỗi server, thử lại sau.";
    throw error;
  }
  return res.json() as Promise<T>;
}

// Chức năng: gửi yêu cầu đăng ký tài khoản.
// Input: data - thông tin đăng ký. Output: token và thông tin người dùng.
export async function register(data: RegisterRequest): Promise<AuthResponse> {
  const res = await fetch(`${API_BASE}/api/v1/auth/register`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(data),
  });
  return handleResponse<AuthResponse>(res);
}

// Chức năng: tạo header JSON và Bearer token cho request.
// Input: json - có gửi JSON hay không. Output: tập header HTTP.
function authHeaders(json = false): HeadersInit {
  const token = getToken();
  return {
    ...(json ? { "Content-Type": "application/json" } : {}),
    ...(token ? { Authorization: `Bearer ${token}` } : {}),
  };
}

// Chức năng: đổi mã độ khó từ API sang kiểu hiển thị.
// Input: value - mã độ khó. Output: Easy, Medium hoặc Hard.
const difficulty = (value: number): DifficultyLevel => ({ 1: "Easy", 2: "Medium", 3: "Hard", 4: "Hard" })[value] as DifficultyLevel || "Easy";
// Chức năng: đổi mã trạng thái từ API sang kiểu hiển thị.
// Input: value - mã trạng thái. Output: Draft, Published hoặc Archived.
const status = (value: number): RecipeStatus => (["Draft", "Published", "Archived"][value] || "Draft") as RecipeStatus;

// Chức năng: ánh xạ dữ liệu công thức từ API sang mô hình frontend.
// Input: raw - công thức theo hợp đồng API. Output: đối tượng Recipe dùng bởi giao diện.
export function mapRecipe(raw: ApiRecipe): Recipe {
  const imageRows: ApiImage[] = raw.images || (raw.primaryImageUrl
    ? [{ id: `primary-${raw.id}`, originalUrl: raw.primaryImageUrl, isPrimary: true }]
    : []);
  return {
    id: raw.id,
    title: raw.title,
    slug: raw.slug,
    description: raw.description,
    instructions: raw.instructions || "",
    prepTime: raw.prepTime,
    cookTime: raw.cookTime,
    servings: raw.servings,
    difficultyLevel: difficulty(raw.difficulty),
    status: status(raw.status),
    images: imageRows.map((image) => ({
      id: image.id,
      url: image.originalUrl || image.thumbnailUrl || image.mediumUrl || "",
      isPrimary: image.isPrimary,
      caption: image.altText,
      rowVersion: image.rowVersion,
    })),
    ingredients: (raw.ingredients || []).map((item) => ({
      id: item.id,
      amount: item.quantity == null ? "" : String(item.quantity),
      unit: item.unit || "",
      name: item.name,
      rowVersion: item.rowVersion,
    })),
    steps: (raw.steps || []).map((step) => ({
      id: step.id,
      stepNumber: step.stepNumber,
      title: step.title,
      description: step.description,
      timerMinutes: step.timerMinutes,
      imageUrl: step.imageUrl,
      rowVersion: step.rowVersion,
    })),
    nutrition: raw.nutrition ? {
      calories: raw.nutrition.calories,
      protein: raw.nutrition.protein,
      carbs: raw.nutrition.carbohydrates,
      fat: raw.nutrition.fat,
    } : undefined,
    author: {
      id: raw.authorId,
      displayName: raw.authorName,
      userName: raw.authorName,
      email: "",
      avatarUrl: "https://images.unsplash.com/photo-1535713875002-d1d0cf377fde?w=200&auto=format&fit=crop&q=80",
      role: "User",
      createdAt: raw.createdAt,
    },
    category: {
      id: raw.categoryId,
      name: raw.categoryName,
      slug: raw.categoryName.toLowerCase().replace(/\s+/g, "-"),
      description: "",
      icon: "🍽️",
      recipeCount: 0,
    },
    createdAt: raw.createdAt,
    updatedAt: raw.updatedAt || raw.createdAt,
    viewsCount: 0,
    likesCount: 0,
    rowVersion: raw.rowVersion,
  };
}

// Chức năng: lấy danh sách công thức công khai hoặc của người đang đăng nhập.
// Input: mine - true nếu chỉ lấy công thức của tôi. Output: mảng Recipe.
// Chức năng: lấy chi tiết một công thức theo slug.
// Input: slug - định danh trên URL. Output: một Recipe đầy đủ.
export async function getRecipes(mine = false): Promise<Recipe[]> {
  const res = await fetch(`${API_BASE}/api/v1/recipes?page=1&pageSize=100${mine ? "&mine=true" : ""}`, { headers: authHeaders() });
  const page = await handleResponse<RecipePage>(res);
  return page.items.map(mapRecipe);
}

export async function getRecipe(slug: string): Promise<Recipe> {
  const res = await fetch(`${API_BASE}/api/v1/recipes/${encodeURIComponent(slug)}`, { headers: authHeaders() });
  return mapRecipe(await handleResponse<ApiRecipe>(res));
}

// Chức năng: chuyển dữ liệu form thành payload tạo/cập nhật công thức.
// Input: data - dữ liệu Recipe chưa đầy đủ. Output: payload đúng hợp đồng API.
function toMutation(data: Partial<Recipe>) {
  const levels: Record<DifficultyLevel, number> = { Easy: 1, Medium: 2, Hard: 3 };
  return {
    title: data.title,
    description: data.description,
    instructions: data.instructions || "",
    prepTime: data.prepTime,
    cookTime: data.cookTime,
    servings: data.servings,
    difficulty: levels[data.difficultyLevel || "Easy"],
    categoryId: data.category?.id,
    nutrition: data.nutrition ? {
      calories: data.nutrition.calories,
      protein: data.nutrition.protein,
      carbohydrates: data.nutrition.carbs,
      fat: data.nutrition.fat,
    } : null,
  };
}

// Chức năng: tạo công thức nháp mới.
// Input: data - thông tin công thức. Output: id, slug, trạng thái và RowVersion.
export async function createRecipe(data: Partial<Recipe>): Promise<RecipeMutationResponse> {
  const res = await fetch(`${API_BASE}/api/v1/recipes`, { method: "POST", headers: authHeaders(true), body: JSON.stringify(toMutation(data)) });
  return handleResponse<RecipeMutationResponse>(res);
}

// Chức năng: cập nhật thông tin chính của công thức.
// Input: id, data và rowVersion. Output: thông tin công thức sau cập nhật.
export async function updateRecipeApi(id: string, data: Partial<Recipe>, rowVersion: string): Promise<RecipeMutationResponse> {
  const res = await fetch(`${API_BASE}/api/v1/recipes/${id}`, { method: "PUT", headers: authHeaders(true), body: JSON.stringify({ ...toMutation(data), rowVersion }) });
  return handleResponse<RecipeMutationResponse>(res);
}

// Chức năng: xuất bản, hủy xuất bản, lưu trữ hoặc khôi phục công thức.
// Input: id, action và rowVersion. Output: trạng thái và RowVersion mới.
export async function mutateRecipe(id: string, action: "publish" | "unpublish" | "archive" | "unarchive", rowVersion: string): Promise<RecipeMutationResponse> {
  const res = await fetch(`${API_BASE}/api/v1/recipes/${id}/${action}`, { method: "POST", headers: authHeaders(true), body: JSON.stringify({ rowVersion }) });
  return handleResponse<RecipeMutationResponse>(res);
}

// Chức năng: xóa mềm một công thức.
// Input: id và rowVersion. Output: không có dữ liệu khi thành công.
export async function deleteRecipeApi(id: string, rowVersion: string): Promise<void> {
  const res = await fetch(`${API_BASE}/api/v1/recipes/${id}`, { method: "DELETE", headers: authHeaders(true), body: JSON.stringify({ rowVersion }) });
  if (!res.ok) await handleResponse<never>(res);
}

// Chức năng: thêm nguyên liệu vào công thức.
// Input: recipeId và item. Output: nguyên liệu đã lưu.
export async function addIngredient(recipeId: string, item: Ingredient): Promise<IngredientResponse> {
  const quantity = item.amount === "" ? null : Number(item.amount);
  const res = await fetch(`${API_BASE}/api/v1/recipes/${recipeId}/ingredients`, { method: "POST", headers: authHeaders(true), body: JSON.stringify({ name: item.name, quantity, unit: quantity == null ? null : item.unit, notes: null }) });
  return handleResponse<IngredientResponse>(res);
}

// Chức năng: cập nhật nội dung và thứ tự nguyên liệu.
// Input: recipeId, item và index. Output: nguyên liệu sau cập nhật.
export async function updateIngredientApi(recipeId: string, item: Ingredient, index: number): Promise<IngredientResponse> {
  const quantity = item.amount === "" ? null : Number(item.amount);
  const res = await fetch(`${API_BASE}/api/v1/recipes/${recipeId}/ingredients/${item.id}`, { method: "PUT", headers: authHeaders(true), body: JSON.stringify({ name: item.name, quantity, unit: quantity == null ? null : item.unit, notes: null, orderIndex: index, rowVersion: item.rowVersion }) });
  return handleResponse<IngredientResponse>(res);
}

// Chức năng: xóa mềm một nguyên liệu.
// Input: recipeId và item chứa id, RowVersion. Output: không có dữ liệu.
export async function deleteIngredientApi(recipeId: string, item: Ingredient): Promise<void> {
  const res = await fetch(`${API_BASE}/api/v1/recipes/${recipeId}/ingredients/${item.id}`, { method: "DELETE", headers: authHeaders(true), body: JSON.stringify({ rowVersion: item.rowVersion }) });
  if (!res.ok) await handleResponse<never>(res);
}

// Chức năng: thêm bước thực hiện.
// Input: recipeId và item. Output: bước thực hiện đã lưu.
export async function addStep(recipeId: string, item: RecipeStep): Promise<StepResponse> {
  const res = await fetch(`${API_BASE}/api/v1/recipes/${recipeId}/steps`, { method: "POST", headers: authHeaders(true), body: JSON.stringify({ title: item.title || null, description: item.description, timerMinutes: item.timerMinutes || null, imageUrl: item.imageUrl || null }) });
  return handleResponse<StepResponse>(res);
}

// Chức năng: cập nhật một bước thực hiện.
// Input: recipeId và item. Output: bước thực hiện sau cập nhật.
export async function updateStepApi(recipeId: string, item: RecipeStep): Promise<StepResponse> {
  const res = await fetch(`${API_BASE}/api/v1/recipes/${recipeId}/steps/${item.id}`, { method: "PUT", headers: authHeaders(true), body: JSON.stringify({ title: item.title || null, description: item.description, timerMinutes: item.timerMinutes || null, imageUrl: item.imageUrl || null, rowVersion: item.rowVersion }) });
  return handleResponse<StepResponse>(res);
}

// Chức năng: xóa mềm một bước thực hiện.
// Input: recipeId và item chứa id, RowVersion. Output: không có dữ liệu.
export async function deleteStepApi(recipeId: string, item: RecipeStep): Promise<void> {
  const res = await fetch(`${API_BASE}/api/v1/recipes/${recipeId}/steps/${item.id}`, { method: "DELETE", headers: authHeaders(true), body: JSON.stringify({ rowVersion: item.rowVersion }) });
  if (!res.ok) await handleResponse<never>(res);
}

// Chức năng: tải ảnh công thức lên máy chủ.
// Input: recipeId, file và altText tùy chọn. Output: thông tin ảnh đã lưu.
export async function uploadRecipeImage(recipeId: string, file: File, altText?: string): Promise<ImageResponse> {
  const form = new FormData();
  form.append("file", file);
  if (altText) form.append("altText", altText);
  const res = await fetch(`${API_BASE}/api/v1/recipes/${recipeId}/images`, { method: "POST", headers: authHeaders(), body: form });
  return handleResponse<ImageResponse>(res);
}

// Chức năng: xóa ảnh công thức.
// Input: recipeId và item chứa id, RowVersion. Output: không có dữ liệu.
export async function deleteImageApi(recipeId: string, item: RecipeImage): Promise<void> {
  const res = await fetch(`${API_BASE}/api/v1/recipes/${recipeId}/images/${item.id}`, { method: "DELETE", headers: authHeaders(true), body: JSON.stringify({ rowVersion: item.rowVersion }) });
  if (!res.ok) await handleResponse<never>(res);
}

// Chức năng: đặt một ảnh làm ảnh đại diện.
// Input: recipeId và ảnh chứa id, RowVersion. Output: thông tin ảnh đại diện mới.
export async function setPrimaryImageApi(recipeId: string, item: Pick<RecipeImage, "id" | "rowVersion">): Promise<ImageResponse> {
  const res = await fetch(`${API_BASE}/api/v1/recipes/${recipeId}/images/${item.id}/primary`, { method: "POST", headers: authHeaders(true), body: JSON.stringify({ rowVersion: item.rowVersion }) });
  return handleResponse<ImageResponse>(res);
}
