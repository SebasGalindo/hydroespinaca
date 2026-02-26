'use client';

import ReactMarkdown from 'react-markdown';
import remarkGfm from 'remark-gfm';
import { Prism as SyntaxHighlighter } from 'react-syntax-highlighter';
import { oneDark } from 'react-syntax-highlighter/dist/esm/styles/prism';
import type { ChatMessage } from '@hydroespinaca/shared';
import { ChatBubbleWeb } from '../atoms/ChatBubbleWeb';

interface MarkdownMessageProps {
    message: ChatMessage;
}

/**
 * Molecule — renders a completed model message with full Markdown support.
 *
 * Composes `ChatBubbleWeb` + `react-markdown` + `react-syntax-highlighter`.
 * Used for all finalised messages stored in the session history.
 * During streaming use `StreamingMessage` instead.
 */
export function MarkdownMessage({ message }: MarkdownMessageProps) {
    if (message.role === 'user') {
        return (
            <ChatBubbleWeb role="user">
                <span className="whitespace-pre-wrap">{message.content}</span>
            </ChatBubbleWeb>
        );
    }

    if (message.role === 'system') {
        return (
            <ChatBubbleWeb role="system">
                <span>{message.content}</span>
            </ChatBubbleWeb>
        );
    }

    return (
        <ChatBubbleWeb role="model">
            <div className="prose prose-sm max-w-none overflow-x-auto prose-p:leading-relaxed prose-p:my-1 prose-headings:my-2 prose-ul:my-1 prose-ol:my-1 prose-li:my-0.5 prose-code:before:content-none prose-code:after:content-none prose-pre:bg-transparent prose-pre:p-0">
                <ReactMarkdown
                    remarkPlugins={[remarkGfm]}
                    components={{
                        // Syntax-highlighted code blocks
                        code(props) {
                            const { children, className, ...rest } = props;
                            const match = /language-(\w+)/.exec(className || '');
                            const isBlock = match !== null;
                            return isBlock ? (
                                <SyntaxHighlighter
                                    style={oneDark}
                                    language={match[1]}
                                    PreTag="div"
                                    customStyle={{
                                        margin: '6px 0',
                                        borderRadius: '8px',
                                        fontSize: '0.8rem',
                                    }}
                                >
                                    {String(children).replace(/\n$/, '')}
                                </SyntaxHighlighter>
                            ) : (
                                <code
                                    className="font-mono text-xs bg-gray-200 px-1.5 py-0.5 rounded text-green-900 border border-gray-300"
                                    {...rest}
                                >
                                    {children}
                                </code>
                            );
                        },
                        // Open links in a new tab
                        a({ children, href, ...props }) {
                            return (
                                <a
                                    href={href}
                                    target="_blank"
                                    rel="noopener noreferrer"
                                    className="text-green-600 hover:text-green-500 underline"
                                    {...props}
                                >
                                    {children}
                                </a>
                            );
                        },
                    }}
                >
                    {message.content}
                </ReactMarkdown>
            </div>
        </ChatBubbleWeb>
    );
}
