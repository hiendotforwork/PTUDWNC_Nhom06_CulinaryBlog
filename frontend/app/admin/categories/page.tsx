"use client";

import React, { FormEvent, useState } from "react";
import Link from "next/link";
import { Pencil, PlusCircle, Save, Trash2, X } from "lucide-react";
import { useApp } from "../../context/AppContext";
import { Button } from "../../components/ui/Button";
import { Input, Textarea } from "../../components/ui/Input";
import { Category } from "../../lib/types";

export default function ManageCategoriesPage() {
  const { currentUser, categories, updateCategory, deleteCategory } = useApp();
  const [editingId, setEditingId] = useState<string | null>(null);
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [error, setError] = useState("");
  const [isSaving, setIsSaving] = useState(false);
  const [deletingId, setDeletingId] = useState<string | null>(null);

  const startEditing = (category: Category) => {
    setEditingId(category.id);
    setName(category.name);
    setDescription(category.description);
    setError("");
  };

  const cancelEditing = () => {
    setEditingId(null);
    setName("");
    setDescription("");
    setError("");
  };

  const handleUpdate = async (event: FormEvent<HTMLFormElement>, id: string) => {
    event.preventDefault();
    setError("");
    setIsSaving(true);
    try {
      await updateCategory(id, { name: name.trim(), description: description.trim() });
      cancelEditing();
    } catch (requestError: unknown) {
      setError(getErrorMessage(requestError, "Không thể cập nhật danh mục."));
    } finally {
      setIsSaving(false);
    }
  };

  const handleDelete = async (category: Category) => {
    if (!window.confirm(`Bạn có chắc muốn xóa danh mục "${category.name}"?`)) return;
    setError("");
    setDeletingId(category.id);
    try {
      await deleteCategory(category.id);
    } catch (requestError: unknown) {
      setError(getErrorMessage(requestError, "Không thể xóa danh mục."));
    } finally {
      setDeletingId(null);
    }
  };

  if (currentUser?.role !== "Admin") {
    return (
      <main className="max-w-3xl mx-auto px-4 sm:px-6 py-16">
        <section className="rounded-3xl border border-[#DCD8D2] dark:border-[#3D3934] bg-white dark:bg-[#24211E] p-8 sm:p-12 text-center">
          <h1 className="font-serif text-2xl font-bold text-[#463F3A] dark:text-[#F5F3EF]">
            Chỉ quản trị viên mới có quyền quản lý danh mục
          </h1>
          <p className="mt-3 text-sm text-[#8A817C] dark:text-[#A8A29E]">
            Hãy đăng nhập bằng tài khoản Admin để tiếp tục.
          </p>
          <Link href="/login" className="inline-block mt-6 text-sm font-semibold text-[#C98F7D] hover:underline">
            Đăng nhập
          </Link>
        </section>
      </main>
    );
  }

  const managedCategories = categories.filter((category) => category.slug !== "all");

  return (
    <main className="max-w-5xl mx-auto px-4 sm:px-6 py-8 sm:py-12">
      <div className="flex flex-col sm:flex-row sm:items-end justify-between gap-4 mb-8">
        <div>
          <p className="text-xs font-bold uppercase tracking-wider text-[#C98F7D]">Quản trị</p>
          <h1 className="mt-2 font-serif text-3xl font-bold text-[#463F3A] dark:text-[#F5F3EF]">
            Quản lý danh mục
          </h1>
          <p className="mt-2 text-sm text-[#8A817C] dark:text-[#A8A29E]">
            Chỉnh sửa thông tin hoặc xóa danh mục chưa có công thức.
          </p>
        </div>
        <Link href="/admin/categories/new">
          <Button leftIcon={<PlusCircle className="w-4 h-4" />}>Tạo danh mục</Button>
        </Link>
      </div>

      {error && (
        <p role="alert" className="mb-5 rounded-xl bg-[#F9EBEB] px-4 py-3 text-sm text-[#9E4A4A]">
          {error}
        </p>
      )}

      <div className="overflow-hidden rounded-2xl border border-[#DCD8D2] dark:border-[#3D3934] bg-white dark:bg-[#24211E]">
        {managedCategories.length === 0 ? (
          <p className="p-8 text-center text-sm text-[#8A817C]">Chưa có danh mục nào.</p>
        ) : (
          <ul className="divide-y divide-[#DCD8D2] dark:divide-[#3D3934]">
            {managedCategories.map((category) => (
              <li key={category.id} className="p-5 sm:p-6">
                {editingId === category.id ? (
                  <form onSubmit={(event) => void handleUpdate(event, category.id)} className="flex flex-col gap-4">
                    <Input
                      label="Tên danh mục"
                      value={name}
                      onChange={(event) => setName(event.target.value)}
                      minLength={2}
                      maxLength={50}
                      required
                      helperText={`URL slug được giữ nguyên: ${category.slug}`}
                    />
                    <Textarea
                      label="Mô tả"
                      value={description}
                      onChange={(event) => setDescription(event.target.value)}
                      rows={3}
                      maxLength={500}
                    />
                    <div className="flex gap-2">
                      <Button type="submit" isLoading={isSaving} leftIcon={<Save className="w-4 h-4" />}>
                        Lưu thay đổi
                      </Button>
                      <Button type="button" variant="outline" onClick={cancelEditing} leftIcon={<X className="w-4 h-4" />}>
                        Hủy
                      </Button>
                    </div>
                  </form>
                ) : (
                  <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
                    <div className="min-w-0">
                      <h2 className="font-semibold text-[#463F3A] dark:text-[#F5F3EF]">{category.name}</h2>
                      <p className="mt-1 text-xs text-[#8A817C]">{category.slug}</p>
                      {category.description && (
                        <p className="mt-2 text-sm text-[#8A817C] dark:text-[#A8A29E]">{category.description}</p>
                      )}
                      <p className="mt-2 text-xs text-[#8A817C]">{category.recipeCount} công thức đã xuất bản</p>
                    </div>
                    <div className="flex shrink-0 gap-2">
                      <Button
                        type="button"
                        variant="outline"
                        size="sm"
                        onClick={() => startEditing(category)}
                        leftIcon={<Pencil className="w-3.5 h-3.5" />}
                      >
                        Sửa
                      </Button>
                      <Button
                        type="button"
                        variant="destructive"
                        size="sm"
                        isLoading={deletingId === category.id}
                        disabled={deletingId !== null}
                        onClick={() => void handleDelete(category)}
                        leftIcon={<Trash2 className="w-3.5 h-3.5" />}
                      >
                        Xóa
                      </Button>
                    </div>
                  </div>
                )}
              </li>
            ))}
          </ul>
        )}
      </div>
    </main>
  );
}

function getErrorMessage(error: unknown, fallback: string): string {
  if (error instanceof Error) return error.message;
  if (typeof error === "object" && error !== null && "message" in error && typeof error.message === "string")
    return error.message;
  return fallback;
}
