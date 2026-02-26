/**
 * Atom — animated blinking cursor `|` shown while the LLM is streaming.
 *
 * Rendered inline so it appears right after the last token in
 * `StreamingMessage`. Disappears once `visible` becomes false.
 */
export function TypingCursor({ visible }: { visible: boolean }) {
    if (!visible) return null;
    return (
        <span
            className="inline-block w-[2px] h-4 bg-green-500 rounded-sm ml-0.5 align-middle animate-pulse"
            aria-hidden="true"
        />
    );
}
