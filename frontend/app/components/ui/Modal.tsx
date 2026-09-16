"use client";

import React, { useEffect, useRef } from "react";
import { X } from "lucide-react";
import { clsx } from "clsx";
import { twMerge } from "tailwind-merge";

export interface ModalProps {
  isOpen: boolean;
  onClose: () => void;
  title?: string;
  description?: string;
  size?: "sm" | "md" | "lg" | "xl";
  children: React.ReactNode;
  footer?: React.ReactNode;
}

export const Modal: React.FC<ModalProps> = ({
  isOpen,
  onClose,
  title,
  description,
  size = "md",
  children,
  footer,
}) => {
  const modalRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === "Escape" && isOpen) {
        onClose();
      }
    };
    if (isOpen) {
      document.body.style.overflow = "hidden";
      window.addEventListener("keydown", handleKeyDown);
    }
    return () => {
      document.body.style.overflow = "unset";
      window.removeEventListener("keydown", handleKeyDown);
    };
  }, [isOpen, onClose]);

  if (!isOpen) return null;

  const sizeStyles = {
    sm: "max-w-sm",
    md: "max-w-lg",
    lg: "max-w-2xl",
    xl: "max-w-4xl",
  };

  return (
    <div
      role="dialog"
      aria-modal="true"
      className="fixed inset-0 z-50 flex items-center justify-center p-4 sm:p-6"
    >
      {/* Backdrop */}
      <div
        className="fixed inset-0 bg-[#463F3A]/60 backdrop-blur-sm transition-opacity"
        onClick={onClose}
      />

      {/* Modal Dialog Card (Double Bezel Architecture) */}
      <div
        ref={modalRef}
        className={twMerge(
          clsx(
            "relative w-full z-10 p-1.5 rounded-[1.75rem] bg-[#8A817C]/20 border border-white/20 shadow-2xl transition-all",
            sizeStyles[size]
          )
        )}
      >
        <div className="bg-white dark:bg-[#24211E] rounded-[calc(1.75rem-6px)] overflow-hidden shadow-inner flex flex-col max-h-[90vh]">
          {/* Modal Header */}
          {(title || description) && (
            <div className="px-6 pt-5 pb-4 border-b border-[#DCD8D2]/60 dark:border-[#3D3934] flex items-start justify-between">
              <div>
                {title && (
                  <h3 className="font-serif text-xl font-bold text-[#463F3A] dark:text-[#F5F3EF]">
                    {title}
                  </h3>
                )}
                {description && (
                  <p className="text-xs text-[#8A817C] dark:text-[#A8A29E] mt-1">{description}</p>
                )}
              </div>
              <button
                onClick={onClose}
                aria-label="Đóng"
                className="p-1 rounded-full text-[#8A817C] hover:text-[#463F3A] hover:bg-[#F4F3EE] dark:hover:bg-[#3D3934] transition-colors"
              >
                <X className="w-5 h-5" />
              </button>
            </div>
          )}

          {/* Modal Content */}
          <div className="px-6 py-5 overflow-y-auto flex-1 text-[#463F3A] dark:text-[#EAE6DF] text-sm leading-relaxed">
            {children}
          </div>

          {/* Modal Footer */}
          {footer && (
            <div className="px-6 py-4 bg-[#FAF9F6] dark:bg-[#1E1B19] border-t border-[#DCD8D2]/60 dark:border-[#3D3934] flex items-center justify-end gap-3">
              {footer}
            </div>
          )}
        </div>
      </div>
    </div>
  );
};
