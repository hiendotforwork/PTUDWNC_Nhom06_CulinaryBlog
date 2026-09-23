import { AuthResponse, RegisterRequest, ApiError } from "./types";

const API_BASE = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5058";

async function handleResponse<T>(res: Response): Promise<T> {
  if (!res.ok) {
    const error: ApiError = await res.json().catch(() => ({
      statusCode: res.status,
      message: res.status === 500 ? "Lỗi server, thử lại sau." : "Lỗi không xác định từ máy chủ.",
    }));
    if (res.status === 500 && !error.message) {
      error.message = "Lỗi server, thử lại sau.";
    }
    throw error;
  }
  return res.json();
}

export async function register(data: RegisterRequest): Promise<AuthResponse> {
  const res = await fetch(`${API_BASE}/api/v1/auth/register`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(data),
  });
  return handleResponse<AuthResponse>(res);
}
