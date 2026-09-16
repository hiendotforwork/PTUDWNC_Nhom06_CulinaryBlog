"use client";

import React from "react";
import { CheckCircle2, AlertCircle, AlertTriangle, Info, X } from "lucide-react";
import { ToastMessage } from "../../lib/types";

export interface ToastProps {
  toasts: ToastMessage[];
  onDismiss: (id: string) => void;
}

export const ToastContainer: React.FC<ToastProps> = ({ toasts, onDismiss }) => {
  if (toasts.length === 0) return null;

  return (
    <div
      aria-live="polite"
      aria-atomic="true"
      className="fixed z-50 bottom-4 right-4 left-4 sm:left-auto sm:w-96 flex flex-col gap-2.5 pointer-events-none"
    >
      {toasts.map((toast) => {
        let icon = <CheckCircle2 className="w-5 h-5 text-[#6B8E6B] shrink-0" />;
        let borderClass = "border-[#6B8E6B]/30 bg-[#FAFBF9]";

        if (toast.type === "error") {
          icon = <AlertCircle className="w-5 h-5 text-[#B85C5C] shrink-0" />;
          borderClass = "border-[#B85C5C]/30 bg-[#FDF9F9]";
        } else if (toast.type === "warning") {
          icon = <AlertTriangle className="w-5 h-5 text-[#C89B3C] shrink-0" />;
          borderClass = "border-[#C89B3C]/30 bg-[#FDFBF7]";
        } else if (toast.type === "info") {
          icon = <Info className="w-5 h-5 text-[#6B8499] shrink-0" />;
          borderClass = "border-[#6B8499]/30 bg-[#F9FBFC]";
        }

        return (
          <div
            key={toast.id}
            role="alert"
            className={`pointer-events-auto flex items-start gap-3 p-4 rounded-2xl shadow-lg border ${borderClass} text-[#463F3A] transition-all duration-300 animate-in fade-in slide-in-from-bottom-3`}
          >
            {icon}
            <div className="flex-1 text-xs sm:text-sm">
              {toast.title && <p className="font-semibold mb-0.5">{toast.title}</p>}
              <p className="leading-snug">{toast.message}</p>
            </div>
            <button
              onClick={() => onDismiss(toast.id)}
              className="p-1 text-[#8A817C] hover:text-[#463F3A] transition-colors cursor-pointer"
              aria-label="Đóng thông báo"
            >
              <X className="w-4 h-4" />
            </button>
          </div>
        );
      })}
    </div>
  );
};
