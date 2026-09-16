"use client";

import React, { InputHTMLAttributes, TextareaHTMLAttributes, forwardRef, useState } from "react";
import { Eye, EyeOff } from "lucide-react";
import { clsx } from "clsx";
import { twMerge } from "tailwind-merge";

export interface InputProps extends InputHTMLAttributes<HTMLInputElement> {
  label?: string;
  error?: string;
  helperText?: string;
  leftIcon?: React.ReactNode;
}

export const Input = forwardRef<HTMLInputElement, InputProps>(
  ({ className, type = "text", label, error, helperText, leftIcon, id, ...props }, ref) => {
    const [showPassword, setShowPassword] = useState(false);
    const inputId = id || (label ? label.toLowerCase().replace(/\s+/g, "-") : undefined);
    const isPassword = type === "password";
    const computedType = isPassword ? (showPassword ? "text" : "password") : type;

    return (
      <div className="w-full flex flex-col gap-1.5">
        {label && (
          <label htmlFor={inputId} className="text-xs font-semibold tracking-wide text-[#463F3A] dark:text-[#EAE6DF] select-none">
            {label}
            {props.required && <span className="text-[#B85C5C] ml-1">*</span>}
          </label>
        )}
        <div className="relative flex items-center">
          {leftIcon && (
            <div className="absolute left-3.5 flex items-center pointer-events-none text-[#8A817C]">
              {leftIcon}
            </div>
          )}
          <input
            id={inputId}
            ref={ref}
            type={computedType}
            className={twMerge(
              clsx(
                "w-full h-11 px-4 text-sm rounded-xl bg-white dark:bg-[#24211E] text-[#463F3A] dark:text-[#F5F3EF] border border-[#DCD8D2] dark:border-[#3D3934] transition-all duration-150 focus:border-[#C98F7D] focus:ring-2 focus:ring-[#C98F7D]/20 focus:outline-none placeholder:text-[#8A817C]/60 disabled:opacity-50 disabled:bg-[#F4F3EE]",
                leftIcon && "pl-10",
                isPassword && "pr-11",
                error && "border-[#B85C5C] focus:border-[#B85C5C] focus:ring-[#B85C5C]/20",
                className
              )
            )}
            {...props}
          />
          {isPassword && (
            <button
              type="button"
              onClick={() => setShowPassword(!showPassword)}
              aria-label={showPassword ? "Ẩn mật khẩu" : "Hiện mật khẩu"}
              className="absolute right-3.5 p-1 text-[#8A817C] hover:text-[#463F3A] dark:hover:text-white transition-colors cursor-pointer"
            >
              {showPassword ? <EyeOff className="w-4 h-4" /> : <Eye className="w-4 h-4" />}
            </button>
          )}
        </div>
        {error && <p className="text-xs font-medium text-[#B85C5C] mt-0.5">{error}</p>}
        {!error && helperText && (
          <p className="text-xs text-[#8A817C] dark:text-[#A8A29E] mt-0.5">{helperText}</p>
        )}
      </div>
    );
  }
);
Input.displayName = "Input";

export interface TextareaProps extends TextareaHTMLAttributes<HTMLTextAreaElement> {
  label?: string;
  error?: string;
  helperText?: string;
}

export const Textarea = forwardRef<HTMLTextAreaElement, TextareaProps>(
  ({ className, label, error, helperText, id, rows = 4, ...props }, ref) => {
    const textareaId = id || (label ? label.toLowerCase().replace(/\s+/g, "-") : undefined);

    return (
      <div className="w-full flex flex-col gap-1.5">
        {label && (
          <label htmlFor={textareaId} className="text-xs font-semibold tracking-wide text-[#463F3A] dark:text-[#EAE6DF] select-none">
            {label}
            {props.required && <span className="text-[#B85C5C] ml-1">*</span>}
          </label>
        )}
        <textarea
          id={textareaId}
          ref={ref}
          rows={rows}
          className={twMerge(
            clsx(
              "w-full p-3 text-sm rounded-xl bg-white dark:bg-[#24211E] text-[#463F3A] dark:text-[#F5F3EF] border border-[#DCD8D2] dark:border-[#3D3934] transition-all duration-150 focus:border-[#C98F7D] focus:ring-2 focus:ring-[#C98F7D]/20 focus:outline-none placeholder:text-[#8A817C]/60 disabled:opacity-50",
              error && "border-[#B85C5C] focus:border-[#B85C5C] focus:ring-[#B85C5C]/20",
              className
            )
          )}
          {...props}
        />
        {error && <p className="text-xs font-medium text-[#B85C5C] mt-0.5">{error}</p>}
        {!error && helperText && (
          <p className="text-xs text-[#8A817C] dark:text-[#A8A29E] mt-0.5">{helperText}</p>
        )}
      </div>
    );
  }
);
Textarea.displayName = "Textarea";
