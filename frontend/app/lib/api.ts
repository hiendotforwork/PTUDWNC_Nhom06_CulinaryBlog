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

export async function register(data: RegisterRequest): Promise<AuthResponse> {
  const res = await fetch(`${API_BASE}/api/v1/auth/register`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(data),
  });
  return handleResponse<AuthResponse>(res);
}

function authHeaders(json = false): HeadersInit {
  const token = getToken();
  return {
    ...(json ? { "Content-Type": "application/json" } : {}),
    ...(token ? { Authorization: `Bearer ${token}` } : {}),
  };
}

const difficulty = (value: number): DifficultyLevel => ({ 1: "Easy", 2: "Medium", 3: "Hard", 4: "Hard" })[value] as DifficultyLevel || "Easy";
const status = (value: number): RecipeStatus => (["Draft", "Published", "Archived"][value] || "Draft") as RecipeStatus;

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

export async function getRecipes(mine = false): Promise<Recipe[]> {
  const res = await fetch(`${API_BASE}/api/v1/recipes?page=1&pageSize=100${mine ? "&mine=true" : ""}`, { headers: authHeaders() });
  const page = await handleResponse<RecipePage>(res);
  return page.items.map(mapRecipe);
}

export async function getRecipe(slug: string): Promise<Recipe> {
  const res = await fetch(`${API_BASE}/api/v1/recipes/${encodeURIComponent(slug)}`, { headers: authHeaders() });
  return mapRecipe(await handleResponse<ApiRecipe>(res));
}

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

export async function createRecipe(data: Partial<Recipe>): Promise<RecipeMutationResponse> {
  const res = await fetch(`${API_BASE}/api/v1/recipes`, { method: "POST", headers: authHeaders(true), body: JSON.stringify(toMutation(data)) });
  return handleResponse<RecipeMutationResponse>(res);
}

export async function updateRecipeApi(id: string, data: Partial<Recipe>, rowVersion: string): Promise<RecipeMutationResponse> {
  const res = await fetch(`${API_BASE}/api/v1/recipes/${id}`, { method: "PUT", headers: authHeaders(true), body: JSON.stringify({ ...toMutation(data), rowVersion }) });
  return handleResponse<RecipeMutationResponse>(res);
}

export async function mutateRecipe(id: string, action: "publish" | "unpublish" | "archive" | "unarchive", rowVersion: string): Promise<RecipeMutationResponse> {
  const res = await fetch(`${API_BASE}/api/v1/recipes/${id}/${action}`, { method: "POST", headers: authHeaders(true), body: JSON.stringify({ rowVersion }) });
  return handleResponse<RecipeMutationResponse>(res);
}

export async function deleteRecipeApi(id: string, rowVersion: string): Promise<void> {
  const res = await fetch(`${API_BASE}/api/v1/recipes/${id}`, { method: "DELETE", headers: authHeaders(true), body: JSON.stringify({ rowVersion }) });
  if (!res.ok) await handleResponse<never>(res);
}

export async function addIngredient(recipeId: string, item: Ingredient): Promise<IngredientResponse> {
  const quantity = item.amount === "" ? null : Number(item.amount);
  const res = await fetch(`${API_BASE}/api/v1/recipes/${recipeId}/ingredients`, { method: "POST", headers: authHeaders(true), body: JSON.stringify({ name: item.name, quantity, unit: quantity == null ? null : item.unit, notes: null }) });
  return handleResponse<IngredientResponse>(res);
}

export async function updateIngredientApi(recipeId: string, item: Ingredient, index: number): Promise<IngredientResponse> {
  const quantity = item.amount === "" ? null : Number(item.amount);
  const res = await fetch(`${API_BASE}/api/v1/recipes/${recipeId}/ingredients/${item.id}`, { method: "PUT", headers: authHeaders(true), body: JSON.stringify({ name: item.name, quantity, unit: quantity == null ? null : item.unit, notes: null, orderIndex: index, rowVersion: item.rowVersion }) });
  return handleResponse<IngredientResponse>(res);
}

export async function deleteIngredientApi(recipeId: string, item: Ingredient): Promise<void> {
  const res = await fetch(`${API_BASE}/api/v1/recipes/${recipeId}/ingredients/${item.id}`, { method: "DELETE", headers: authHeaders(true), body: JSON.stringify({ rowVersion: item.rowVersion }) });
  if (!res.ok) await handleResponse<never>(res);
}

export async function addStep(recipeId: string, item: RecipeStep): Promise<StepResponse> {
  const res = await fetch(`${API_BASE}/api/v1/recipes/${recipeId}/steps`, { method: "POST", headers: authHeaders(true), body: JSON.stringify({ title: item.title || null, description: item.description, timerMinutes: item.timerMinutes || null, imageUrl: item.imageUrl || null }) });
  return handleResponse<StepResponse>(res);
}

export async function updateStepApi(recipeId: string, item: RecipeStep): Promise<StepResponse> {
  const res = await fetch(`${API_BASE}/api/v1/recipes/${recipeId}/steps/${item.id}`, { method: "PUT", headers: authHeaders(true), body: JSON.stringify({ title: item.title || null, description: item.description, timerMinutes: item.timerMinutes || null, imageUrl: item.imageUrl || null, rowVersion: item.rowVersion }) });
  return handleResponse<StepResponse>(res);
}

export async function deleteStepApi(recipeId: string, item: RecipeStep): Promise<void> {
  const res = await fetch(`${API_BASE}/api/v1/recipes/${recipeId}/steps/${item.id}`, { method: "DELETE", headers: authHeaders(true), body: JSON.stringify({ rowVersion: item.rowVersion }) });
  if (!res.ok) await handleResponse<never>(res);
}

export async function uploadRecipeImage(recipeId: string, file: File, altText?: string): Promise<ImageResponse> {
  const form = new FormData();
  form.append("file", file);
  if (altText) form.append("altText", altText);
  const res = await fetch(`${API_BASE}/api/v1/recipes/${recipeId}/images`, { method: "POST", headers: authHeaders(), body: form });
  return handleResponse<ImageResponse>(res);
}

export async function deleteImageApi(recipeId: string, item: RecipeImage): Promise<void> {
  const res = await fetch(`${API_BASE}/api/v1/recipes/${recipeId}/images/${item.id}`, { method: "DELETE", headers: authHeaders(true), body: JSON.stringify({ rowVersion: item.rowVersion }) });
  if (!res.ok) await handleResponse<never>(res);
}

export async function setPrimaryImageApi(recipeId: string, item: Pick<RecipeImage, "id" | "rowVersion">): Promise<ImageResponse> {
  const res = await fetch(`${API_BASE}/api/v1/recipes/${recipeId}/images/${item.id}/primary`, { method: "POST", headers: authHeaders(true), body: JSON.stringify({ rowVersion: item.rowVersion }) });
  return handleResponse<ImageResponse>(res);
}