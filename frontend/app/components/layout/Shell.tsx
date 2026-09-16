"use client";

import React from "react";
import { Header } from "./Header";
import { Footer } from "./Footer";
import { ToastContainer } from "../ui/Toast";
import { useApp } from "../../context/AppContext";

export const Shell: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const { toasts, removeToast } = useApp();

  return (
    <div className="flex flex-col min-h-[100dvh] bg-[#F4F3EE] dark:bg-[#181614] text-[#463F3A] dark:text-[#F5F3EF] selection:bg-[#E8C4BB] selection:text-[#60413A]">
      <Header />
      <main className="flex-1 w-full">{children}</main>
      <Footer />
      <ToastContainer toasts={toasts} onDismiss={removeToast} />
    </div>
  );
};
