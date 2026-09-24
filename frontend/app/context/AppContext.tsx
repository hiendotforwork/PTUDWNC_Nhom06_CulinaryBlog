// Tệp này quản lý state dùng chung của ứng dụng; phần Recipe kết nối giao diện với API công thức.
// Chức năng Recipe: tải lại danh sách (reloadRecipes), tạo (addRecipe), cập nhật (updateRecipe),
// xóa (deleteRecipe), xuất bản/hủy xuất bản (publishRecipe) và lưu trữ/khôi phục (archiveRecipe).

"use client";

import React, { createContext, useContext, useState, useEffect, ReactNode } from "react";
import { Recipe, User, ToastMessage, ToastType, Category, AuthResponse } from "../lib/types";
import { MOCK_CATEGORIES, MOCK_USERS } from "../lib/mockData";
import {
  register as apiRegister,
  login as apiLogin,
  getRecipes,
  getRecipe,
  createRecipe,
  updateRecipeApi,
  mutateRecipe,
  deleteRecipeApi,
  addIngredient,
  updateIngredientApi,
  deleteIngredientApi,
  addStep,
  updateStepApi,
  deleteStepApi,
  uploadRecipeImage,
  deleteImageApi,
  setPrimaryImageApi,
} from "../lib/api";
import * as auth from "../lib/auth";

interface AppContextType {
  currentUser: User | null;
  setCurrentUser: (user: User | null) => void;
  recipes: Recipe[];
  categories: Category[];
  favorites: string[];
  toggleFavorite: (recipeId: string) => void;
  addRecipe: (recipeData: Omit<Recipe, "id" | "slug" | "createdAt" | "updatedAt" | "viewsCount" | "likesCount" | "author">) => Promise<Recipe>;
  updateRecipe: (id: string, recipeData: Partial<Recipe>) => Promise<void>;
  deleteRecipe: (id: string) => Promise<void>;
  publishRecipe: (id: string) => Promise<void>;
  archiveRecipe: (id: string) => Promise<void>;
  login: (email: string, password?: string) => Promise<boolean>;
  register: (data: { displayName: string; email: string; userName: string; password?: string }) => Promise<boolean>;
  logout: () => void;
  updateProfile: (data: { displayName?: string; bio?: string; avatarUrl?: string }) => void;
  toasts: ToastMessage[];
  showToast: (type: ToastType, message: string, title?: string) => void;
  removeToast: (id: string) => void;
}

const AppContext = createContext<AppContextType | undefined>(undefined);

// Standalone ID generator functions outside of component render
function generateToastId(): string {
  return `toast-${Date.now()}-${Math.random().toString(36).slice(2, 7)}`;
}


function generateUserId(): string {
  return `usr-${Date.now()}`;
}


export const AppProvider = ({ children }: { children: ReactNode }) => {
  // Start with null; restore from localStorage via useEffect below
  const [currentUser, setCurrentUser] = useState<User | null>(null);

  // Restore auth state after hydration so server and browser render the same first frame.
  useEffect(() => {
    const timer = window.setTimeout(() => {
      const stored = auth.getStoredUser();
      if (stored) {
        setCurrentUser({
          id: stored.id,
          displayName: stored.displayName,
          userName: stored.userName,
          email: stored.email,
          avatarUrl: stored.avatarUrl || "https://images.unsplash.com/photo-1535713875002-d1d0cf377fde?w=200&auto=format&fit=crop&q=80",
          bio: stored.bio,
          role: (stored.roles?.includes("Author") ? "User" : (stored.roles?.[0] as "User" | "Admin")) || "User",
          createdAt: stored.createdAt || new Date().toISOString(),
        });
      }
    }, 0);
    return () => window.clearTimeout(timer);
  }, []);
  const [recipes, setRecipes] = useState<Recipe[]>([]);

  const [categories, setCategories] = useState<Category[]>(MOCK_CATEGORIES);

  const [favorites, setFavorites] = useState<string[]>(() => {
    if (typeof window === "undefined") return ["rcp-1", "rcp-2"];
    try {
      const saved = localStorage.getItem("culinary_favorites");
      return saved ? JSON.parse(saved) : ["rcp-1", "rcp-2"];
    } catch {
      return ["rcp-1", "rcp-2"];
    }
  });

  const [toasts, setToasts] = useState<ToastMessage[]>([]);

  // Chức năng: tải công thức công khai và công thức của người dùng rồi hợp nhất kết quả.
  // Input: không có. Output: cập nhật state recipes và categories.
  const reloadRecipes = async () => {
    try {
      const publicRecipes = await getRecipes(false);
      const ownRecipes = auth.getToken() ? await getRecipes(true) : [];
      const merged = [...ownRecipes, ...publicRecipes.filter((x) => !ownRecipes.some((o) => o.id === x.id))];
      setRecipes(merged);
      const unique = new Map(merged.map((x) => [x.category.id, x.category]));
      setCategories([{ id: "all", name: "Tất cả", slug: "all", description: "", icon: "🍽️", recipeCount: merged.length }, ...unique.values()]);
    } catch { /* API may be offline during static development. */ }
  };

  useEffect(() => {
    const timer = window.setTimeout(() => void reloadRecipes(), 0);
    return () => window.clearTimeout(timer);
  }, []);

  const removeToast = (id: string) => {
    setToasts((prev) => prev.filter((t) => t.id !== id));
  };

  const showToast = (type: ToastType, message: string, title?: string) => {
    const id = generateToastId();
    const newToast: ToastMessage = { id, type, message, title };
    setToasts((prev) => [...prev, newToast]);

    setTimeout(() => {
      removeToast(id);
    }, 5000);
  };

  const toggleFavorite = (recipeId: string) => {
    setFavorites((prev) => {
      const exists = prev.includes(recipeId);
      const updated = exists ? prev.filter((id) => id !== recipeId) : [...prev, recipeId];
      try {
        localStorage.setItem("culinary_favorites", JSON.stringify(updated));
      } catch {
        // Ignore
      }
      showToast("info", exists ? "Đã bỏ lưu công thức" : "Đã lưu vào danh sách yêu thích");
      return updated;
    });
  };

  // Chức năng: tạo công thức cùng nguyên liệu, bước làm và ảnh; sau đó xuất bản nếu được chọn.
  // Input: recipeData - dữ liệu form. Output: Recipe đầy đủ vừa tạo.
  const addRecipe = async (recipeData: Omit<Recipe, "id" | "slug" | "createdAt" | "updatedAt" | "viewsCount" | "likesCount" | "author">): Promise<Recipe> => {
    const created = await createRecipe(recipeData);
    for (const item of recipeData.ingredients.filter((x) => x.name.trim())) await addIngredient(created.id, item);
    for (const item of recipeData.steps.filter((x) => x.description.trim())) await addStep(created.id, item);

    const uploadedImages = new Map<string, Awaited<ReturnType<typeof uploadRecipeImage>>>();
    for (const image of recipeData.images) {
      if (image.file) uploadedImages.set(image.id, await uploadRecipeImage(created.id, image.file, image.caption));
    }
    const requestedPrimary = recipeData.images.find((image) => image.isPrimary);
    const uploadedPrimary = requestedPrimary ? uploadedImages.get(requestedPrimary.id) : undefined;
    if (uploadedPrimary && !uploadedPrimary.isPrimary) await setPrimaryImageApi(created.id, uploadedPrimary);

    if (recipeData.status === "Published") await mutateRecipe(created.id, "publish", created.rowVersion);
    const result = await getRecipe(created.slug);
    await reloadRecipes();
    showToast("success", "Công thức đã được lưu thành công!");
    return result;
  };

  // Chức năng: đồng bộ thông tin chính, nguyên liệu, bước làm, ảnh và trạng thái công thức.
  // Input: id và recipeData. Output: Promise hoàn tất sau khi tải lại danh sách.
  const updateRecipe = async (id: string, recipeData: Partial<Recipe>) => {
    const current = recipes.find((x) => x.id === id);
    if (!current) throw new Error("Không tìm thấy công thức");

    const existing = await getRecipe(current.slug);
    if (!existing.rowVersion) throw new Error("Thiếu RowVersion của công thức");
    const updated = await updateRecipeApi(id, recipeData, existing.rowVersion);

    if (recipeData.ingredients) {
      for (const oldItem of existing.ingredients) {
        if (!recipeData.ingredients.some((item) => item.id === oldItem.id) && oldItem.rowVersion) {
          await deleteIngredientApi(id, oldItem);
        }
      }
      for (const [index, item] of recipeData.ingredients.entries()) {
        if (!item.name.trim()) continue;
        if (item.rowVersion) await updateIngredientApi(id, item, index);
        else await addIngredient(id, item);
      }
    }

    if (recipeData.steps) {
      for (const oldItem of existing.steps) {
        if (!recipeData.steps.some((item) => item.id === oldItem.id) && oldItem.rowVersion) {
          await deleteStepApi(id, oldItem);
        }
      }
      for (const item of recipeData.steps) {
        if (!item.description.trim()) continue;
        if (item.rowVersion) await updateStepApi(id, item);
        else await addStep(id, item);
      }
    }

    if (recipeData.images) {
      for (const oldImage of existing.images) {
        if (!recipeData.images.some((image) => image.id === oldImage.id) && oldImage.rowVersion) {
          await deleteImageApi(id, oldImage);
        }
      }
      const uploadedImages = new Map<string, Awaited<ReturnType<typeof uploadRecipeImage>>>();
      for (const image of recipeData.images) {
        if (image.file) uploadedImages.set(image.id, await uploadRecipeImage(id, image.file, image.caption));
      }
      const requestedPrimary = recipeData.images.find((image) => image.isPrimary);
      const primaryTarget = requestedPrimary?.rowVersion
        ? requestedPrimary
        : requestedPrimary
          ? uploadedImages.get(requestedPrimary.id)
          : undefined;
      const currentPrimaryId = existing.images.find((image) => image.isPrimary)?.id;
      if (primaryTarget?.rowVersion && primaryTarget.id !== currentPrimaryId) {
        await setPrimaryImageApi(id, primaryTarget);
      }
    }

    if (recipeData.status === "Published" && existing.status !== "Published") {
      await mutateRecipe(id, "publish", updated.rowVersion);
    } else if (recipeData.status === "Draft" && existing.status === "Published") {
      await mutateRecipe(id, "unpublish", updated.rowVersion);
    }
    await reloadRecipes();
    showToast("success", "Đã cập nhật công thức thành công!");
  };
  // Chức năng: lấy thông báo từ lỗi API để hiển thị toast an toàn.
  // Input: error và thông báo fallback. Output: chuỗi thông báo dễ hiểu.
  const mutationErrorMessage = (error: unknown, fallback: string) => {
    if (error instanceof Error) return error.message;
    if (typeof error === "object" && error !== null && "message" in error) {
      const message = (error as { message?: unknown }).message;
      if (typeof message === "string" && message.trim()) return message;
    }
    return fallback;
  };

  // Chức năng: xóa mềm công thức hiện tại.
  // Input: id - mã công thức. Output: Promise hoàn tất sau khi tải lại danh sách.
  const deleteRecipe = async (id: string) => {
    const current = recipes.find((x) => x.id === id);
    if (!current?.rowVersion) return;
    try {
      await deleteRecipeApi(id, current.rowVersion);
      await reloadRecipes();
      showToast("info", "Đã xóa công thức");
    } catch (error: unknown) {
      showToast("error", mutationErrorMessage(error, "Không thể xóa công thức."));
    }
  };

  // Chức năng: xuất bản, hủy xuất bản; công thức lưu trữ sẽ được khôi phục trước khi xuất bản.
  // Input: id - mã công thức. Output: Promise hoàn tất sau khi cập nhật.
  const publishRecipe = async (id: string) => {
    const current = recipes.find((x) => x.id === id);
    if (!current?.rowVersion) return;
    try {
      if (current.status === "Archived") {
        const restored = await mutateRecipe(id, "unarchive", current.rowVersion);
        await mutateRecipe(id, "publish", restored.rowVersion);
        showToast("success", "Đã khôi phục và xuất bản lại công thức!");
      } else if (current.status === "Published") {
        await mutateRecipe(id, "unpublish", current.rowVersion);
        showToast("success", "Đã hủy xuất bản");
      } else {
        await mutateRecipe(id, "publish", current.rowVersion);
        showToast("success", "Công thức đã được xuất bản công khai!");
      }
      await reloadRecipes();
    } catch (error: unknown) {
      showToast("error", mutationErrorMessage(error, "Không thể cập nhật trạng thái xuất bản."));
    }
  };

  // Chức năng: chuyển đổi giữa trạng thái lưu trữ và bản nháp.
  // Input: id - mã công thức. Output: Promise hoàn tất sau khi cập nhật.
  const archiveRecipe = async (id: string) => {
    const current = recipes.find((x) => x.id === id);
    if (!current?.rowVersion) return;
    try {
      const restoring = current.status === "Archived";
      await mutateRecipe(id, restoring ? "unarchive" : "archive", current.rowVersion);
      await reloadRecipes();
      showToast("warning", restoring ? "Đã khôi phục công thức về bản nháp" : "Đã lưu trữ công thức");
    } catch (error: unknown) {
      showToast("error", mutationErrorMessage(error, "Không thể cập nhật trạng thái lưu trữ."));
    }
  };
  const login = async (email: string, password?: string): Promise<boolean> => {
    if (!password) {
      showToast("error", "Vui lòng nhập mật khẩu");
      return false;
    }

    try {
      const response = await apiLogin({ email, password });

      // Store tokens and user
      auth.setToken(response.accessToken);
      auth.setRefreshToken(response.refreshToken);
      auth.setStoredUser(response.user);

      // Convert AuthUser to User for AppContext
      const user: User = {
        id: response.user.id,
        displayName: response.user.displayName,
        userName: response.user.userName,
        email: response.user.email,
        avatarUrl: response.user.avatarUrl || "https://images.unsplash.com/photo-1535713875002-d1d0cf377fde?w=200&auto=format&fit=crop&q=80",
        bio: response.user.bio,
        role: (response.user.roles?.includes("Author") ? "User" : (response.user.roles?.[0] as "User" | "Admin")) || "User",
        createdAt: new Date().toISOString(),
      };

      setCurrentUser(user);
      await reloadRecipes();
      showToast("success", `Chào mừng trở lại, ${user.displayName}!`);
      return true;
    } catch (error: unknown) {
      // Handle network errors (fetch throws TypeError)
      if (error instanceof TypeError && error.message.includes("fetch")) {
        showToast("error", "Không thể kết nối server");
        return false;
      }

      // Handle API errors
      const apiError = error as {
        statusCode?: number;
        errorCode?: string;
        extensions?: { code?: string; retryAfterSeconds?: number; unlockAt?: string };
        message?: string;
      };

      // Account locked (423)
      if (apiError.statusCode === 423) {
        const unlockAt = apiError.extensions?.unlockAt;
        const retryAfter = apiError.extensions?.retryAfterSeconds;
        let message = "Tài khoản đã bị khóa do đăng nhập sai nhiều lần.";
        if (retryAfter) {
          const minutes = Math.ceil(retryAfter / 60);
          message = `Tài khoản đã bị khóa. Vui lòng thử lại sau ${minutes} phút.`;
        } else if (unlockAt) {
          const unlockTime = new Date(unlockAt).toLocaleTimeString("vi-VN", { hour: "2-digit", minute: "2-digit" });
          message = `Tài khoản đã bị khóa. Vui lòng thử lại sau ${unlockTime}.`;
        }
        showToast("error", message);
        return false;
      }

      // Rate limited (429)
      if (apiError.statusCode === 429) {
        const retryAfter = apiError.extensions?.retryAfterSeconds;
        if (retryAfter) {
          const seconds = Math.ceil(retryAfter);
          showToast("warning", `Quá nhiều yêu cầu. Vui lòng chờ ${seconds} giây.`);
        } else {
          showToast("warning", "Quá nhiều yêu cầu. Vui lòng thử lại sau.");
        }
        return false;
      }

      // Invalid credentials (401) - generic message for security
      if (apiError.statusCode === 401 || apiError.errorCode === "AUTH_INVALID_CREDENTIALS") {
        showToast("error", "Email hoặc mật khẩu không đúng.");
        return false;
      }

      // Validation error (422)
      if (apiError.statusCode === 422) {
        showToast("error", "Thông tin đăng nhập không hợp lệ.");
        return false;
      }

      // Default error
      showToast("error", apiError.message || "Đăng nhập thất bại. Vui lòng thử lại.");
      return false;
    }
  };

  const register = async (data: { displayName: string; email: string; userName: string; password?: string }) => {
    try {
      const response: AuthResponse = await apiRegister({
        email: data.email,
        userName: data.userName,
        displayName: data.displayName,
        password: data.password || "",
      });

      // Store tokens and user
      auth.setToken(response.accessToken);
      auth.setRefreshToken(response.refreshToken);
      auth.setStoredUser(response.user);

      // Convert AuthUser to User for AppContext
      const user: User = {
        id: response.user.id,
        displayName: response.user.displayName,
        userName: response.user.userName,
        email: response.user.email,
        avatarUrl: response.user.avatarUrl || "https://images.unsplash.com/photo-1535713875002-d1d0cf377fde?w=200&auto=format&fit=crop&q=80",
        bio: response.user.bio,
        role: (response.user.roles?.includes("Author") ? "User" : (response.user.roles?.[0] as "User" | "Admin")) || "User",
        createdAt: new Date().toISOString(),
      };

      setCurrentUser(user);
      await reloadRecipes();
      showToast("success", `Đăng ký thành công! Chào mừng ${user.displayName}`);
      return true;
    } catch (error: unknown) {
      // Handle network errors (fetch throws TypeError)
      if (error instanceof TypeError && error.message.includes("fetch")) {
        const msg = "Không thể kết nối server";
        showToast("error", msg);
        throw { message: msg };
      }
      const apiError = error as { statusCode?: number; errorCode?: string; error?: string; errors?: Array<{ field: string; message: string }>; message?: string };
      const errCode = apiError.errorCode || apiError.error;
      if (errCode === "AUTH_EMAIL_EXISTS") {
        throw { field: "email", message: "Email đã được sử dụng" };
      }
      if (errCode === "AUTH_USERNAME_EXISTS") {
        throw { field: "userName", message: "Tên đăng nhập đã được sử dụng" };
      }
      if (apiError.errors && apiError.errors.length > 0) {
        const fieldErrors: Record<string, string> = {};
        for (const err of apiError.errors) {
          let fieldName = err.field.charAt(0).toLowerCase() + err.field.slice(1);
          if (fieldName.toLowerCase() === "username") fieldName = "userName";
          if (fieldName.toLowerCase() === "displayname") fieldName = "displayName";
          fieldErrors[fieldName] = err.message;
        }
        const firstError = apiError.errors[0];
        let firstFieldName = firstError.field.charAt(0).toLowerCase() + firstError.field.slice(1);
        if (firstFieldName.toLowerCase() === "username") firstFieldName = "userName";
        if (firstFieldName.toLowerCase() === "displayname") firstFieldName = "displayName";
        throw { fieldErrors, field: firstFieldName, message: firstError.message };
      }
      if (apiError.statusCode === 500) {
        const msg = "Lỗi server, thử lại sau";
        showToast("error", msg);
        throw { message: msg };
      }
      const message = apiError.message || "Đăng ký thất bại. Vui lòng thử lại.";
      showToast("error", message);
      throw { message };
    }
  };

  const logout = () => {
    auth.clearAuth();
    setCurrentUser(null);
    showToast("info", "Đã đăng xuất");
  };

  const updateProfile = (data: { displayName?: string; bio?: string; avatarUrl?: string }) => {
    if (!currentUser) return;
    const updated: User = {
      ...currentUser,
      ...(data.displayName ? { displayName: data.displayName } : {}),
      ...(data.bio !== undefined ? { bio: data.bio } : {}),
      ...(data.avatarUrl ? { avatarUrl: data.avatarUrl } : {}),
    };
    setCurrentUser(updated);
    showToast("success", "Hồ sơ đã được cập nhật thành công!");
  };

  return (
    <AppContext.Provider
      value={{
        currentUser,
        setCurrentUser,
        recipes,
        categories,
        favorites,
        toggleFavorite,
        addRecipe,
        updateRecipe,
        deleteRecipe,
        publishRecipe,
        archiveRecipe,
        login,
        register,
        logout,
        updateProfile,
        toasts,
        showToast,
        removeToast,
      }}
    >
      {children}
    </AppContext.Provider>
  );
};

export const useApp = () => {
  const context = useContext(AppContext);
  if (!context) {
    throw new Error("useApp must be used within an AppProvider");
  }
  return context;
};
