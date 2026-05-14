// ProductResultCard.tsx
import { useEffect, useRef, useState } from "react";
import { createPortal } from "react-dom";
import JsBarcode from "jsbarcode";
import { Button } from "@/components/ui/button";
import {
  Card,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import { Dialog, DialogContent, DialogTitle } from "@/components/ui/dialog";
import { Barcode, Eye, Copy, Check } from "lucide-react";

// ─────────────────────────────────────────────
// Tipos
// ─────────────────────────────────────────────
interface ProductResult {
  productCode: string;
  productName: string;
  category: string;
  imageUrl: string;
}

const mockProduct: ProductResult = {
  productCode: "CB000000018BR",
  productName: "Caneca",
  category: "Utilidades",
  imageUrl:
    "https://images.unsplash.com/photo-1577937927133-66ef06acdf18?q=80&w=1200&auto=format&fit=crop",
};

// ─────────────────────────────────────────────
// Helpers
// ─────────────────────────────────────────────
function detectBarcodeFormat(code: string): string {
  if (/^\d+$/.test(code)) {
    const len = code.length;
    if (len === 8)  return "EAN8";
    if (len === 12) return "UPC";
    if (len === 13) return "EAN13";
    if (len === 14) return "ITF14";
  }
  return "CODE128";
}

const FORMAT_LABELS: Record<string, string> = {
  EAN13: "EAN-13", EAN8: "EAN-8",
  UPC: "UPC-A", ITF14: "ITF-14", CODE128: "CODE 128",
};

function formatProductCode(code: string): string {
  if (/^\d+$/.test(code)) {
    const len = code.length;
    if (len === 13) return `${code[0]} · ${code.slice(1, 7)} · ${code.slice(7)}`;
    if (len === 8)  return `${code.slice(0, 4)} · ${code.slice(4)}`;
    if (len === 12) return `${code.slice(0, 5)} · ${code.slice(5, 10)} · ${code.slice(10)}`;
    if (len === 14) return `${code.slice(0, 5)} · ${code.slice(5, 8)} · ${code.slice(8)}`;
  }
  return code.match(/.{1,4}/g)?.join(" · ") ?? code;
}

// ─────────────────────────────────────────────
// Skeleton
// ─────────────────────────────────────────────
function ProductCardSkeleton() {
  return (
    <Card className="w-full max-w-sm overflow-hidden pt-0">
      <Skeleton className="aspect-video w-full rounded-none" />
      <CardHeader className="space-y-3">
        <Skeleton className="h-8 w-40" />
        <Skeleton className="h-6 w-32" />
        <Skeleton className="h-4 w-3/4" />
      </CardHeader>
      <CardFooter className="flex gap-2">
        <Skeleton className="h-10 flex-1" />
        <Skeleton className="h-10 flex-1" />
      </CardFooter>
    </Card>
  );
}

// ─────────────────────────────────────────────
// Modal de visualização da imagem
// ─────────────────────────────────────────────
function ProductPreviewDialog({
  product, open, onOpenChange,
}: {
  product: ProductResult;
  open: boolean;
  onOpenChange: (open: boolean) => void;
}) {
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent showCloseButton={false} className="max-w-6xl border-none bg-transparent p-0 shadow-none">
        <DialogTitle className="sr-only">Visualização do produto</DialogTitle>
        <div className="cursor-pointer" onClick={() => onOpenChange(false)}>
          <img
            src={product.imageUrl}
            alt={product.productName}
            className="max-h-[90vh] w-auto max-w-full rounded-xl object-contain"
          />
        </div>
      </DialogContent>
    </Dialog>
  );
}

// ─────────────────────────────────────────────
// Scanner overlay
//
// Design:
//   • Fundo branco puro — essencial para leitores ópticos
//   • Barcode ocupa toda a largura com margens mínimas
//   • Linha de scan animada (vermelho laser) atravessa o barcode
//   • Marcadores de canto (estilo scanner profissional)
//   • Topo: status "Pronto para leitura" + badge do formato
//   • Base: código formatado + hint de fechar
// ─────────────────────────────────────────────
function BarcodeOverlay({ code, onClose }: { code: string; onClose: () => void }) {
  const svgRef       = useRef<SVGSVGElement>(null);
  const barcodeRef   = useRef<HTMLDivElement>(null);
  const format       = detectBarcodeFormat(code);
  const formatLabel  = FORMAT_LABELS[format] ?? format;

  useEffect(() => {
    if (!svgRef.current) return;

    const margin      = 28;                              // px de cada lado
    const available   = window.innerWidth - margin * 2;

    const moduleCount =
      format === "EAN13"  ? 95
      : format === "EAN8"  ? 67
      : format === "UPC"   ? 95
      : format === "ITF14" ? 135
      : Math.max(60, code.length * 11);

    const barWidth  = Math.max(1, Math.floor(available / moduleCount));
    const barHeight = 130;  // px — suficiente para qualquer leitor laser ou câmera

    const opts = (fmt: string) => ({
      format: fmt,
      displayValue: false,    // código exibido manualmente abaixo, mais bonito
      width: barWidth,
      height: barHeight,
      margin,
      background: "#ffffff",
      lineColor: "#000000",
    });

    try {
      JsBarcode(svgRef.current, code, opts(format));
    } catch {
      JsBarcode(svgRef.current, code, opts("CODE128"));
    }
  }, [code, format]);

  // Fecha com Escape
  useEffect(() => {
    const h = (e: KeyboardEvent) => { if (e.key === "Escape") onClose(); };
    document.addEventListener("keydown", h);
    return () => document.removeEventListener("keydown", h);
  }, [onClose]);

  // Trava scroll
  useEffect(() => {
    const prev = document.body.style.overflow;
    document.body.style.overflow = "hidden";
    return () => { document.body.style.overflow = prev; };
  }, []);

  // Marcadores de canto do scanner
  const cornerSize   = 22;
  const cornerWeight = 3;
  const cornerColor  = "#111";
  const corners = [
    { top: 0, left: 0,  borderTop: `${cornerWeight}px solid ${cornerColor}`, borderLeft:  `${cornerWeight}px solid ${cornerColor}` },
    { top: 0, right: 0, borderTop: `${cornerWeight}px solid ${cornerColor}`, borderRight: `${cornerWeight}px solid ${cornerColor}` },
    { bottom: 0, left: 0,  borderBottom: `${cornerWeight}px solid ${cornerColor}`, borderLeft:  `${cornerWeight}px solid ${cornerColor}` },
    { bottom: 0, right: 0, borderBottom: `${cornerWeight}px solid ${cornerColor}`, borderRight: `${cornerWeight}px solid ${cornerColor}` },
  ];

  return createPortal(
    <>
      <style>{`
        @keyframes pulseDot {
          0%, 100% { opacity: 1; }
          50% { opacity: 0.3; }
        }
      `}</style>

      <div
        onClick={onClose}
        style={{
          position: "fixed",
          inset: 0,
          zIndex: 99999,
          background: "#ffffff",
          display: "flex",
          flexDirection: "column",
          alignItems: "center",
          justifyContent: "center",
          cursor: "pointer",
          userSelect: "none",
        }}
      >
        {/* ── Topo ── */}
        <div style={{
          position: "absolute",
          top: 28,
          display: "flex",
          flexDirection: "column",
          alignItems: "center",
          gap: 8,
          pointerEvents: "none",
        }}>
          {/* Indicador pulsante + label */}
          <div style={{ display: "flex", alignItems: "center", gap: 7 }}>
            <div style={{
              width: 7,
              height: 7,
              borderRadius: "50%",
              background: "#22c55e",
              animation: "pulseDot 1.4s ease-in-out infinite",
            }} />
            <span style={{
              fontSize: 11,
              fontWeight: 600,
              color: "#555",
              letterSpacing: "0.12em",
              textTransform: "uppercase",
            }}>
              Pronto para leitura
            </span>
          </div>

          {/* Badge do formato */}
          <span style={{
            fontSize: 11,
            fontWeight: 700,
            color: "#999",
            background: "#f5f5f5",
            borderRadius: 20,
            padding: "3px 12px",
            letterSpacing: "0.1em",
          }}>
            {formatLabel}
          </span>
        </div>

        {/* ── Barcode + frame ── */}
        <div
          ref={barcodeRef}
          onClick={(e) => e.stopPropagation()}
          style={{
            position: "relative",
            width: "100%",
            cursor: "default",
          }}
        >
          {/* Marcadores de canto */}
          {corners.map((style, i) => (
            <div
              key={i}
              style={{
                position: "absolute",
                width: cornerSize,
                height: cornerSize,
                zIndex: 2,
                pointerEvents: "none",
                ...style,
              }}
            />
          ))}

          <svg
            ref={svgRef}
            style={{ display: "block", width: "100%" }}
          />
        </div>

        {/* ── Base ── */}
        <div style={{
          position: "absolute",
          bottom: 28,
          display: "flex",
          flexDirection: "column",
          alignItems: "center",
          gap: 6,
          pointerEvents: "none",
        }}>
          <span style={{
            fontFamily: "'Courier New', Courier, monospace",
            fontSize: 17,
            fontWeight: 700,
            letterSpacing: "0.14em",
            color: "#111",
          }}>
            {formatProductCode(code)}
          </span>
          <span style={{
            fontSize: 11,
            color: "#ccc",
            letterSpacing: "0.06em",
          }}>
            Toque para fechar
          </span>
        </div>
      </div>
    </>,
    document.body
  );
}

function BarcodeFullscreen({ code, open, onOpenChange }: {
  code: string; open: boolean; onOpenChange: (v: boolean) => void;
}) {
  if (!open) return null;
  return <BarcodeOverlay code={code} onClose={() => onOpenChange(false)} />;
}

// ─────────────────────────────────────────────
// Badge do código com botão copiar
// ─────────────────────────────────────────────
function ProductCodeBadge({ code }: { code: string }) {
  const [copied, setCopied] = useState(false);

  async function handleCopy() {
    await navigator.clipboard.writeText(code);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  }

  return (
    <div className="flex items-center gap-2">
      <div className="flex min-w-0 flex-1 items-center rounded-lg border border-dashed border-primary/40 bg-primary/5 px-3 py-2">
        <span className="truncate font-mono text-base font-bold tracking-widest text-primary">
          {formatProductCode(code)}
        </span>
      </div>
      <Button
        size="icon" variant="ghost"
        className="h-9 w-9 shrink-0 text-muted-foreground hover:text-primary"
        onClick={handleCopy} title="Copiar código"
      >
        {copied
          ? <Check className="h-4 w-4 text-green-500" />
          : <Copy className="h-4 w-4" />}
      </Button>
    </div>
  );
}

// ─────────────────────────────────────────────
// Card do produto
// ─────────────────────────────────────────────
function ProductCard({ product }: { product: ProductResult }) {
  const [previewOpen, setPreviewOpen] = useState(false);
  const [barcodeOpen, setBarcodeOpen] = useState(false);

  return (
    <>
      <Card className="w-full max-w-sm overflow-hidden pt-0 shadow-lg">
        <img
          src={product.imageUrl}
          alt={product.productName}
          className="aspect-video w-full object-cover"
        />
        <CardHeader className="space-y-3">
          <div className="space-y-3">
            <CardTitle className="text-2xl font-bold">{product.productName}</CardTitle>
            <ProductCodeBadge code={product.productCode} />
          </div>
          <CardDescription className="text-sm">
            {product.category || "Sem categoria"}
          </CardDescription>
        </CardHeader>
        <CardFooter className="flex gap-2">
          <Button className="flex-1" onClick={() => setPreviewOpen(true)}>
            <Eye className="mr-2 h-4 w-4" />
            Visualizar
          </Button>
          <Button variant="outline" className="flex-1" onClick={() => setBarcodeOpen(true)}>
            <Barcode className="mr-2 h-4 w-4" />
            Código de Barras
          </Button>
        </CardFooter>
      </Card>

      <ProductPreviewDialog product={product} open={previewOpen} onOpenChange={setPreviewOpen} />
      <BarcodeFullscreen code={product.productCode} open={barcodeOpen} onOpenChange={setBarcodeOpen} />
    </>
  );
}

// ─────────────────────────────────────────────
// Componente principal
// ─────────────────────────────────────────────
export default function ProductResultCard() {
  const [loading, setLoading] = useState(true);
  const [product, setProduct] = useState<ProductResult | null>(null);

  useEffect(() => {
    const timer = setTimeout(() => {
      setProduct(mockProduct);
      setLoading(false);
    }, 2500);
    return () => clearTimeout(timer);
  }, []);

  if (loading) return <ProductCardSkeleton />;
  if (!product) return null;
  return <ProductCard product={product} />;
}