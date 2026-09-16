"use client";

import React, { ButtonHTMLAttributes, forwardRef } from "react";
import { Loader2 } from "lucide-react";
import { clsx } from "clsx";
import { twMerge } from "tailwind-merge";

export interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: "primary" | "secondary" | "outline" | "ghost" | "destructive";
  size?: "sm" | "md" | "lg";
  isLoading?: boolean;
  leftIcon?: React.ReactNode;
  rightIcon?: React.ReactNode;
  nestedIcon?: React.ReactNode; // Button-in-button architecture
}

export const Button = forwardRef<HTMLButtonElement, ButtonProps>(
  (
    {
      className,
      variant = "primary",
      size = "md",
      isLoading = false,
      leftIcon,
      rightIcon,
      nestedIcon,
      children,
      disabled,
      ...props
    },
    ref
  ) => {
    const baseStyles =
      "inline-flex items-center justify-center font-medium transition-all duration-200 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[#C98F7D] focus-visible:ring-offset-2 disabled:opacity-50 disabled:pointer-events-none active:scale-[0.98] select-none cursor-pointer";

    const variantStyles = {
      primary:
        "bg-[#C98F7D] text-white hover:bg-[#B0705E] active:bg-[#965A4B] shadow-sm hover:shadow-md",
      secondary:
        "bg-[#EFECE6] text-[#463F3A] hover:bg-[#E4DFD7] border border-[#DCD8D2] active:bg-[#DCD8D2]",
      outline:
        "bg-transparent border border-[#BCB8B1] text-[#463F3A] hover:border-[#C98F7D] hover:text-[#C98F7D] hover:bg-[#FDF8F6]",
      ghost:
        "bg-transparent text-[#463F3A] hover:bg-[#F3DDD8]/40 hover:text-[#B0705E]",
      destructive:
        "bg-[#B85C5C] text-white hover:bg-[#9E4A4A] active:bg-[#853C3C] shadow-sm",
    };

    const sizeStyles = {
      sm: "text-xs h-8 px-3 rounded-full gap-1.5",
      md: "text-sm h-10 px-4 rounded-full gap-2",
      lg: "text-base h-12 px-6 rounded-full gap-2.5",
    };

    return (
      <button
        ref={ref}
        disabled={disabled || isLoading}
        className={twMerge(
          clsx(
            baseStyles,
            variantStyles[variant],
            sizeStyles[size],
            nestedIcon && "pr-2",
            className
          )
        )}
        {...props}
      >
        {isLoading ? (
          <Loader2 className="w-4 h-4 animate-spin shrink-0" />
        ) : (
          leftIcon && <span className="shrink-0">{leftIcon}</span>
        )}
        <span>{children}</span>
        {!isLoading && rightIcon && <span className="shrink-0">{rightIcon}</span>}

        {/* Button-in-button nested trailing icon architecture */}
        {!isLoading && nestedIcon && (
          <span className="w-7 h-7 rounded-full bg-white/20 dark:bg-black/20 flex items-center justify-center shrink-0 ml-1 transition-transform group-hover:translate-x-0.5">
            {nestedIcon}
          </span>
        )}
      </button>
    );
  }
);

Button.displayName = "Button";
