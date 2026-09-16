"use client";

import React, { useRef, useState } from "react";
import Image from "next/image";
import { UploadCloud, Star, Trash2, Plus, AlertCircle } from "lucide-react";
import { RecipeImage } from "../../lib/types";

export interface ImageUploadZoneProps {
  images: RecipeImage[];
  onChange: (images: RecipeImage[]) => void;
  maxImages?: number;
}

export const ImageUploadZone: React.FC<ImageUploadZoneProps> = ({
  images,
  onChange,
  maxImages = 8,
}) => {
  const [isDragOver, setIsDragOver] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const handleFiles = (files: FileList | null) => {
    if (!files || files.length === 0) return;
    setErrorMessage(null);

    const validExtensions = ["image/jpeg", "image/png", "image/webp", "image/avif"];
    const newImageList: RecipeImage[] = [...images];

    Array.from(files).forEach((file) => {
      if (!validExtensions.includes(file.type)) {
        setErrorMessage("Chỉ hỗ trợ định dạng JPG, PNG, WEBP, hoặc AVIF.");
        return;
      }
      if (file.size > 5 * 1024 * 1024) {
        setErrorMessage("Dung lượng ảnh tối đa là 5MB.");
        return;
      }
      if (newImageList.length >= maxImages) {
        setErrorMessage(`Chỉ được tải lên tối đa ${maxImages} hình ảnh.`);
        return;
      }

      const reader = new FileReader();
      reader.onload = (e) => {
        const url = e.target?.result as string;
        const newImg: RecipeImage = {
          id: `img-${Date.now()}-${Math.random().toString(36).slice(2, 6)}`,
          url,
          isPrimary: newImageList.length === 0, // First image is primary by default
        };
        newImageList.push(newImg);
        onChange([...newImageList]);
      };
      reader.readAsDataURL(file);
    });
  };

  const setPrimaryImage = (id: string) => {
    const updated = images.map((img) => ({
      ...img,
      isPrimary: img.id === id,
    }));
    onChange(updated);
  };

  const removeImage = (id: string) => {
    const remaining = images.filter((img) => img.id !== id);
    if (remaining.length > 0 && !remaining.some((img) => img.isPrimary)) {
      remaining[0].isPrimary = true;
    }
    onChange(remaining);
  };

  return (
    <div className="flex flex-col gap-4">
      {/* Drag & Drop Upload Zone */}
      <div
        onDragOver={(e) => {
          e.preventDefault();
          setIsDragOver(true);
        }}
        onDragLeave={() => setIsDragOver(false)}
        onDrop={(e) => {
          e.preventDefault();
          setIsDragOver(false);
          handleFiles(e.dataTransfer.files);
        }}
        onClick={() => fileInputRef.current?.click()}
        className={`w-full p-6 sm:p-8 rounded-2xl border-2 border-dashed transition-all duration-200 cursor-pointer flex flex-col items-center justify-center text-center ${
          isDragOver
            ? "border-[#C98F7D] bg-[#FDF8F6] dark:bg-[#2A2320]"
            : "border-[#DCD8D2] dark:border-[#3D3934] bg-[#FAF9F6] dark:bg-[#24211E] hover:border-[#C98F7D]/70"
        }`}
      >
        <input
          ref={fileInputRef}
          type="file"
          accept="image/jpeg,image/png,image/webp,image/avif"
          multiple
          onChange={(e) => handleFiles(e.target.files)}
          className="hidden"
        />
        <div className="w-12 h-12 rounded-full bg-white dark:bg-[#2F2B27] shadow-sm border border-[#DCD8D2] dark:border-[#3D3934] flex items-center justify-center text-[#C98F7D] mb-3">
          <UploadCloud className="w-6 h-6" />
        </div>
        <p className="text-sm font-semibold text-[#463F3A] dark:text-[#F5F3EF]">
          Kéo và thả hình ảnh món ăn vào đây
        </p>
        <p className="text-xs text-[#8A817C] dark:text-[#A8A29E] mt-1">
          hoặc <span className="text-[#C98F7D] font-medium underline">chọn từ thiết bị của bạn</span>
        </p>
        <p className="text-[11px] text-[#8A817C] mt-2">
          Hỗ trợ: JPG, PNG, WEBP, AVIF (Tối đa 5MB mỗi ảnh, tối đa {maxImages} ảnh)
        </p>
      </div>

      {errorMessage && (
        <div className="flex items-center gap-2 text-xs font-medium text-[#B85C5C] bg-[#F9EBEB] p-3 rounded-xl">
          <AlertCircle className="w-4 h-4 shrink-0" />
          <span>{errorMessage}</span>
        </div>
      )}

      {/* Uploaded Thumbnails Grid */}
      {images.length > 0 && (
        <div>
          <div className="flex items-center justify-between mb-2">
            <span className="text-xs font-bold text-[#463F3A] dark:text-[#EAE6DF] uppercase tracking-wider">
              Hình ảnh đã tải ({images.length}/{maxImages})
            </span>
            <span className="text-[11px] text-[#8A817C]">
              ★ Ảnh đầu tiên hoặc được đánh dấu sao sẽ làm ảnh bìa
            </span>
          </div>

          <div className="grid grid-cols-2 sm:grid-cols-4 gap-3">
            {images.map((img) => (
              <div
                key={img.id}
                className={`group relative aspect-video rounded-xl overflow-hidden border-2 transition-all ${
                  img.isPrimary
                    ? "border-[#C98F7D] shadow-md ring-2 ring-[#C98F7D]/30"
                    : "border-[#DCD8D2] dark:border-[#3D3934]"
                }`}
              >
                <Image src={img.url} alt="Thumbnail công thức" fill className="object-cover" />

                {/* Primary Badge */}
                {img.isPrimary && (
                  <div className="absolute top-1.5 left-1.5 z-10 px-2 py-0.5 rounded-full bg-[#C98F7D] text-white text-[10px] font-bold flex items-center gap-1 shadow-xs">
                    <Star className="w-3 h-3 fill-white" />
                    <span>Ảnh bìa</span>
                  </div>
                )}

                {/* Actions Overlay */}
                <div className="absolute inset-0 bg-black/50 opacity-0 group-hover:opacity-100 transition-opacity flex items-center justify-center gap-2">
                  {!img.isPrimary && (
                    <button
                      type="button"
                      onClick={() => setPrimaryImage(img.id)}
                      title="Đặt làm ảnh bìa"
                      className="p-1.5 rounded-full bg-white text-[#463F3A] hover:text-[#C98F7D] transition-colors"
                    >
                      <Star className="w-3.5 h-3.5" />
                    </button>
                  )}
                  <button
                    type="button"
                    onClick={() => removeImage(img.id)}
                    title="Xóa ảnh"
                    className="p-1.5 rounded-full bg-white text-[#B85C5C] hover:bg-[#F9EBEB] transition-colors"
                  >
                    <Trash2 className="w-3.5 h-3.5" />
                  </button>
                </div>
              </div>
            ))}

            {/* Quick Add More Tile */}
            {images.length < maxImages && (
              <button
                type="button"
                onClick={() => fileInputRef.current?.click()}
                className="aspect-video rounded-xl border border-dashed border-[#DCD8D2] dark:border-[#3D3934] bg-[#FAF9F6] dark:bg-[#24211E] hover:border-[#C98F7D] flex flex-col items-center justify-center gap-1 text-[#8A817C] hover:text-[#C98F7D] transition-colors cursor-pointer"
              >
                <Plus className="w-5 h-5" />
                <span className="text-xs font-medium">Thêm ảnh</span>
              </button>
            )}
          </div>
        </div>
      )}
    </div>
  );
};
