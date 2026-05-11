import type { ReactNode } from 'react';
import { Button } from "@/components/ui/button"
import {
  Sheet,
  SheetContent,
  SheetFooter,
  SheetHeader,
  SheetTitle,
  SheetTrigger,
} from "@/components/ui/sheet"

import {
  House,
  Package,
  Phone,
  Menu,
} from "lucide-react";

type LayoutProps = {
  children: ReactNode;
  title: string;
};

export default function Layout({ children, title }: LayoutProps) {
  return (
    <div className="min-h-screen flex flex-col bg-gray-50">
      {/* HEADER */}
      <header className="bg-blue-50 h-17 px-4 flex items-center justify-between shadow-sm">

        {/* Lado esquerdo: logo + título */}
        <div className="flex items-center gap-3">
          {/* Logo */}
          <div className="w-12 h-12 bg-blue-100 rounded-md flex items-center justify-center">
            <img
              src="/logo.png"
              alt="Logo"
              className="w-full h-full object-contain"
            />
          </div>

          {/* Título */}
          <h1 className="text-xl font-bold text-gray-800">
            Title
          </h1>
        </div>

        {/* Lado direito: botão do menu */}
        <Sheet>
          <SheetTrigger asChild>
            <Button variant="outline" size="icon">
              <Menu className="w-5 h-5" />
            </Button>
          </SheetTrigger>

          <SheetContent className="flex flex-col">
            <SheetHeader>
              <SheetTitle>Menu</SheetTitle>
            </SheetHeader>

            {/* Botões do menu */}
            <div className="flex flex-col gap-2 mt-6 flex-1">
              <Button
                variant="ghost"
                className="w-full justify-start h-12 gap-2"
              >
                <House className="w-5 h-5" />
                Página Inicial
              </Button>

              <Button
                variant="ghost"
                className="w-full justify-start h-12 gap-2"
              >
                <Package className="w-5 h-5" />
                Produtos
              </Button>

              <Button
                variant="ghost"
                className="w-full justify-start h-12 gap-2"
              >
                <Phone className="w-5 h-5" />
                Contato
              </Button>
            </div>
            {/* Rodapé do Sheet */}
            <SheetFooter className="border-t pt-4">
              <p className="text-xs text-gray-500 text-center w-full">
                Software dedicado a busca visual de produtos da Casa Brasileira.
              </p>
            </SheetFooter>
          </SheetContent>
        </Sheet>
      </header>

      {/* MAIN */}
      <main className="flex-1 container mx-auto px-4 py-8">
        {title && (
          <h1 className="text-2xl font-bold text-gray-800 mb-6">
            {title}
          </h1>
        )}
        {children}
      </main>

      {/* FOOTER */}
      <footer className="bg-black/5 h-17" />
    </div>
  );
}