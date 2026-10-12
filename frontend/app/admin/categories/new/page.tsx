"use client";

import React, { FormEvent, useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { ArrowLeft, PlusCircle } from "lucide-react";
import { useApp } from "../../../context/AppContext";
import { Button } from "../../../components/ui/Button";
import { Input, Textarea } from "../../../components/ui/Input";

export default function CreateCategoryPage() {
  const router = useRouter();
  const { currentUser, addCategory } = useApp();
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [error, setError] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setError("");
    setIsSubmitting(true);

    try {
      const category = await addCategory({ name: name.trim(), description: description.trim() });
      router.push(`/categories/${encodeURIComponent(category.slug)}`);
    } catch (requestError: unknown) {
      const message = requestError instanceof Error
        ? requestError.message
        : typeof requestError === "object" && requestError !== null
          && "message" in requestError && typeof requestError.message === "string"
            ? requestError.message
            : "Không thể tạo danh mục. Vui lòng thử lại.";
      setError(message);
    } finally {
      setIsSubmitting(false);
    }
  };

  if (currentUser?.role !== "Admin") {
    return (
      <main className="max-w-3xl mx-auto px-4 sm:px-6 py-16">
        <section className="rounded-3xl border border-[#DCD8D2] dark:border-[#3D3934] bg-white dark:bg-[#24211E] p-8 sm:p-12 text-center">
          <h1 className="font-serif text-2xl font-bold text-[#463F3A] dark:text-[#F5F3EF]">
            Chỉ quản trị viên mới có quyền tạo danh mục
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

  return (
    <main className="max-w-3xl mx-auto px-4 sm:px-6 py-8 sm:py-12">
      <Link
        href="/admin/categories"
        className="inline-flex items-center gap-2 text-sm text-[#8A817C] hover:text-[#C98F7D] transition-colors"
      >
        <ArrowLeft className="w-4 h-4" />
        Về trang chủ
      </Link>

      <section className="mt-6 rounded-3xl border border-[#DCD8D2] dark:border-[#3D3934] bg-white dark:bg-[#24211E] p-6 sm:p-10">
        <div className="mb-8">
          <p className="text-xs font-bold uppercase tracking-wider text-[#C98F7D]">Quản trị danh mục</p>
          <h1 className="mt-2 font-serif text-3xl font-bold text-[#463F3A] dark:text-[#F5F3EF]">
            Tạo danh mục mới
          </h1>
          <p className="mt-2 text-sm text-[#8A817C] dark:text-[#A8A29E]">
            Slug URL sẽ được tự động tạo từ tên danh mục.
          </p>
        </div>

        <form onSubmit={handleSubmit} className="flex flex-col gap-6">
          <Input
            label="Tên danh mục"
            name="name"
            value={name}
            onChange={(event) => setName(event.target.value)}
            minLength={2}
            maxLength={50}
            required
            autoComplete="off"
            placeholder="Ví dụ: Món chay"
            helperText={`${name.trim().length}/50 ký tự`}
          />
          <Textarea
            label="Mô tả"
            name="description"
            value={description}
            onChange={(event) => setDescription(event.target.value)}
            rows={4}
            maxLength={500}
            placeholder="Giới thiệu ngắn về danh mục (không bắt buộc)"
          />

          {error && (
            <p role="alert" className="rounded-xl bg-[#F9EBEB] px-4 py-3 text-sm text-[#9E4A4A]">
              {error}
            </p>
          )}

          <div className="flex flex-wrap gap-3">
            <Button type="submit" isLoading={isSubmitting} leftIcon={<PlusCircle className="w-4 h-4" />}>
              Tạo danh mục
            </Button>
            <Link href="/admin/categories">
              <Button type="button" variant="outline">Hủy</Button>
            </Link>
          </div>
        </form>
      </section>
    </main>
  );
}
