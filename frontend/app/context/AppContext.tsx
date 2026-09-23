"use client";

import React, { createContext, useContext, useState, useEffect, ReactNode } from "react";
import { Recipe, User, ToastMessage, ToastType, Category, AuthResponse } from "../lib/types";
import { INITIAL_RECIPES, MOCK_CATEGORIES, MOCK_USERS } from "../lib/mockData";
import { register as apiRegister } from "../lib/api";
import * as auth from "../lib/auth";

interface AppContextType {
  currentUser: User | null;
  setCurrentUser: (user: User | null) => void;
  recipes: Recipe[];
  categories: Category[];
  favorites: string[];
  toggleFavorite: (recipeId: string) => void;
  addRecipe: (recipeData: Omit<Recipe, "id" | "slug" | "createdAt" | "updatedAt" | "viewsCount" | "likesCount" | "author">) => Recipe;
  updateRecipe: (id: string, recipeData: Partial<Recipe>) => void;
  deleteRecipe: (id: string) => void;
  publishRecipe: (id: string) => void;
  archiveRecipe: (id: string) => void;
  login: (email: string, password?: string) => boolean;
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

function generateRecipeId(): string {
  return `rcp-${Date.now()}`;
}

function generateUserId(): string {
  return `usr-${Date.now()}`;
}

function generateRandomSuffix(): string {
  return Math.random().toString(36).slice(2, 6);
}

export const AppProvider = ({ children }: { children: ReactNode }) => {
  // Start with null; restore from localStorage via useEffect below
  const [currentUser, setCurrentUser] = useState<User | null>(null);

  // Restore auth state from localStorage on mount
  useEffect(() => {
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
  }, []);

  const [recipes, setRecipes] = useState<Recipe[]>(() => {
    if (typeof window === "undefined") return INITIAL_RECIPES;
    try {
      const saved = localStorage.getItem("culinary_recipes");
      return saved ? JSON.parse(saved) : INITIAL_RECIPES;
    } catch {
      return INITIAL_RECIPES;
    }
  });

  const [categories] = useState<Category[]>(MOCK_CATEGORIES);

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

  const saveRecipes = (newRecipes: Recipe[]) => {
    setRecipes(newRecipes);
    try {
      localStorage.setItem("culinary_recipes", JSON.stringify(newRecipes));
    } catch {
      // Ignore localStorage errors
    }
  };

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

  const addRecipe = (
    recipeData: Omit<Recipe, "id" | "slug" | "createdAt" | "updatedAt" | "viewsCount" | "likesCount" | "author">
  ): Recipe => {
    const slug = recipeData.title
      .toLowerCase()
      .normalize("NFD")
      .replace(/[\u0300-\u036f]/g, "")
      .replace(/[đĐ]/g, "d")
      .replace(/[^a-z0-9]+/g, "-")
      .replace(/(^-|-$)/g, "");

    const newRecipe: Recipe = {
      ...recipeData,
      id: generateRecipeId(),
      slug: `${slug}-${generateRandomSuffix()}`,
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
      viewsCount: 1,
      likesCount: 0,
      author: currentUser || MOCK_USERS[0],
    };

    const updated = [newRecipe, ...recipes];
    saveRecipes(updated);
    showToast("success", "Công thức đã được lưu thành công!");
    return newRecipe;
  };

  const updateRecipe = (id: string, recipeData: Partial<Recipe>) => {
    const updated = recipes.map((r) =>
      r.id === id ? { ...r, ...recipeData, updatedAt: new Date().toISOString() } : r
    );
    saveRecipes(updated);
    showToast("success", "Đã cập nhật công thức thành công!");
  };

  const deleteRecipe = (id: string) => {
    const updated = recipes.filter((r) => r.id !== id);
    saveRecipes(updated);
    showToast("info", "Đã xóa công thức");
  };

  const publishRecipe = (id: string) => {
    updateRecipe(id, { status: "Published" });
    showToast("success", "Công thức đã được xuất bản công khai!");
  };

  const archiveRecipe = (id: string) => {
    updateRecipe(id, { status: "Archived" });
    showToast("warning", "Đã lưu trữ công thức (ẩn khỏi trang chủ)");
  };

  const login = (email: string) => {
    const found = MOCK_USERS.find((u) => u.email.toLowerCase() === email.toLowerCase());
    const user = found || {
      id: generateUserId(),
      displayName: email.split("@")[0],
      userName: email.split("@")[0].toLowerCase(),
      email,
      avatarUrl: "https://images.unsplash.com/photo-1535713875002-d1d0cf377fde?w=200&auto=format&fit=crop&q=80",
      bio: "Đầu bếp gia đình nhiệt huyết.",
      role: "User",
      createdAt: new Date().toISOString(),
    };
    setCurrentUser(user);
    showToast("success", `Chào mừng trở lại, ${user.displayName}!`);
    return true;
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
      showToast("success", `Đăng ký thành công! Chào mừng ${user.displayName}`);
      return true;
    } catch (error: unknown) {
      // Handle network errors (fetch throws TypeError)
      if (error instanceof TypeError && error.message.includes("fetch")) {
        const msg = "Không thể kết nối server. Vui lòng kiểm tra kết nối mạng.";
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
        const firstError = apiError.errors[0];
        let fieldName = firstError.field.charAt(0).toLowerCase() + firstError.field.slice(1);
        if (fieldName.toLowerCase() === "username") fieldName = "userName";
        if (fieldName.toLowerCase() === "displayname") fieldName = "displayName";
        throw { field: fieldName, message: firstError.message };
      }
      if (apiError.statusCode === 500) {
        const msg = "Lỗi server, thử lại sau.";
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
