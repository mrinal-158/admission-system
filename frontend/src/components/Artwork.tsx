/** Hand-drawn SVG artwork. Decorative only, so every piece is hidden from screen readers. */

export function Crest({ className = 'brand-crest' }: { className?: string }) {
  return (
    <svg className={className} viewBox="0 0 40 46" aria-hidden="true" focusable="false">
      <path d="M20 1.5 L37.5 7 V22 C37.5 33 29.5 40.5 20 44.5 C10.5 40.5 2.5 33 2.5 22 V7 Z" fill="#00703c" stroke="#fff" strokeWidth="2" />
      {/* Rising sun over an open book. */}
      <circle cx="20" cy="18" r="5" fill="#ffdd00" />
      <g stroke="#ffdd00" strokeWidth="1.6" strokeLinecap="round">
        <path d="M20 8.5v2.5M12.5 12l1.8 1.8M27.5 12l-1.8 1.8M10 18.5h2.5M30 18.5h-2.5" />
      </g>
      <path d="M8.5 24 C13 22 17 22.5 20 25 C23 22.5 27 22 31.5 24 V34 C27 32 23 32.5 20 35 C17 32.5 13 32 8.5 34 Z" fill="#fff" />
      <path d="M20 25 V35" stroke="#00703c" strokeWidth="1.2" />
    </svg>
  );
}

/** The school front: a two-storey block with a clock tower, the national flag and a tree. */
export function SchoolScene({ className = 'notice-art' }: { className?: string }) {
  const windowsTop = [94, 128, 248, 282];
  return (
    <svg className={className} viewBox="0 0 360 222" aria-hidden="true" focusable="false">
      <g stroke="#0b0c0c" strokeWidth="2" strokeLinejoin="round">
        <circle cx="300" cy="30" r="12" fill="#ffdd00" strokeWidth="1.5" />
        {/* Flag of Bangladesh on its pole. */}
        <path d="M26 200 V52" fill="none" />
        <rect x="26" y="54" width="38" height="24" fill="#006a4e" strokeWidth="1.5" />
        <circle cx="43" cy="66" r="7" fill="#f42a41" stroke="none" />
        {/* Main block, parapet and clock tower. */}
        <rect x="170" y="38" width="60" height="40" fill="#f3f2f1" />
        <circle cx="200" cy="56" r="10" fill="#fff" strokeWidth="1.5" />
        <path d="M200 56 V49 M200 56 L205 59" fill="none" strokeWidth="1.5" strokeLinecap="round" />
        <rect x="80" y="82" width="240" height="118" fill="#f3f2f1" />
        <rect x="72" y="70" width="256" height="14" fill="#1d70b8" />
        {windowsTop.map((x) => (
          <g key={x}>
            <rect x={x} y="96" width="24" height="28" fill="#d2e2f1" strokeWidth="1.5" />
            <path d={`M${x + 12} 96 V124`} strokeWidth="1.2" />
            <rect x={x} y="144" width="24" height="28" fill="#d2e2f1" strokeWidth="1.5" />
            <path d={`M${x + 12} 144 V172`} strokeWidth="1.2" />
          </g>
        ))}
        {/* Name plaque, door and steps. */}
        <rect x="168" y="100" width="64" height="18" fill="#fff" strokeWidth="1.5" />
        <path d="M178 109 H222" strokeWidth="2.5" strokeLinecap="round" />
        <path d="M182 200 V146 A18 18 0 0 1 218 146 V200" fill="#00703c" />
        <path d="M200 132 V200" strokeWidth="1.5" />
        <rect x="172" y="196" width="56" height="5" fill="#b1b4b6" strokeWidth="1.5" />
        {/* Tree. */}
        <path d="M340 200 V168" fill="none" />
        <circle cx="340" cy="156" r="17" fill="#00703c" />
        <path d="M0 201 H360" fill="none" />
      </g>
      {/* Path to the door. */}
      <path d="M186 202 H214 L238 222 H162 Z" fill="#e3ddd1" />
    </svg>
  );
}

/** An empty document tray, for a student with no applications yet. */
export function EmptyTray() {
  return (
    <svg viewBox="0 0 110 80" aria-hidden="true" focusable="false">
      <g stroke="#0b0c0c" strokeWidth="2" strokeLinejoin="round" fill="none">
        <rect x="26" y="6" width="58" height="46" fill="#f4f8f3" />
        <path d="M36 20 H74 M36 30 H74 M36 40 H60" stroke="#b1b4b6" />
        <path d="M6 44 H32 L38 56 H72 L78 44 H104 V74 H6 Z" fill="#f3f2f1" />
      </g>
    </svg>
  );
}

/** Roughens stamp edges like ink on paper. Rendered once; stamps refer to it by id. */
export function InkFilter() {
  return (
    <svg width="0" height="0" style={{ position: 'absolute' }} aria-hidden="true" focusable="false">
      <filter id="ink-rough">
        <feTurbulence type="fractalNoise" baseFrequency="0.9" numOctaves="2" seed="7" result="noise" />
        <feDisplacementMap in="SourceGraphic" in2="noise" scale="2.2" />
      </filter>
    </svg>
  );
}
