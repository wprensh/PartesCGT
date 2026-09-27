import { Product } from './product.model';

const MAX_SPECS = 3;

// Datos técnicos que se leen de un vistazo. Micro-ATX va antes que ATX para que gane la coincidencia larga.
const SPEC_PATTERN =
  /\b(\d+(?:[.,]\d+)?\s?(?:TB|GB|MHz|MB\/s|W)|PCIe \d\.\d|DDR\d|AM\d|M\.2|NVMe|SATA|SODIMM|Micro-ATX|ATX|80\+ \w+|TKL|\d+ núcleos)/gi;

/** Especificaciones para la tarjeta: los atributos del producto o, si no tiene, las que se leen del texto. */
export function productSpecs(p: Product): string[] {
  if (p.attributes.length) return p.attributes.slice(0, MAX_SPECS).map(a => a.value);
  return specsFromText(`${p.name} ${p.description}`);
}

/** Una especificación por tipo de unidad (no "115 W" y "550 W" a la vez). */
function specsFromText(text: string): string[] {
  const seenUnits = new Set<string>();
  const specs: string[] = [];
  for (const [match] of text.matchAll(SPEC_PATTERN)) {
    const unit = match.replace(/[\d.,\s]/g, '').toLowerCase();
    if (seenUnits.has(unit)) continue;
    seenUnits.add(unit);
    specs.push(match);
    if (specs.length === MAX_SPECS) break;
  }
  return specs;
}
