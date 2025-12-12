import React from "react";

interface ButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: "primary" | "secondary" | "danger";
  size?: "small" | "medium" | "large";
  isLoading?: boolean;
}

export default function Button({
  children,
  variant = "primary",
  size = "medium",
  isLoading = false,
  disabled,
  className = "",
  ...props
}: ButtonProps) {
  const baseClasses = "button";
  const variantClass = `button--${variant}`;
  const sizeClass = `button--${size}`;
  const loadingClass = isLoading ? "button--loading" : "";

  return (
    <button
      className={`${baseClasses} ${variantClass} ${sizeClass} ${loadingClass} ${className}`}
      disabled={disabled || isLoading}
      {...props}
    >
      {isLoading ? "Loading..." : children}
    </button>
  );
}
