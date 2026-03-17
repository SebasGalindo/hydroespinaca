'use client';

import React, { useState, useEffect } from 'react';
import type { GroupedPermissionResponseDto, PermissionResponseDto } from '@hydroespinaca/shared';
import { ChevronRightIcon } from '@/components/ui/icons/Icons';

interface PermissionTreeProps {
  groupedPermissions: GroupedPermissionResponseDto[];
  selectedPermissionCodes: string[];
  onChange: (permissionCodes: string[]) => void;
  disabled?: boolean;
}

/**
 * PermissionTree - Interactive tree component for selecting permissions grouped by category
 */
export const PermissionTree = React.memo(function PermissionTree({
  groupedPermissions,
  selectedPermissionCodes,
  onChange,
  disabled = false
}: PermissionTreeProps) {
  const [expandedCategories, setExpandedCategories] = useState<Set<string>>(new Set());

  // Expand all categories by default
  useEffect(() => {
    const allCategories = groupedPermissions.map((g: GroupedPermissionResponseDto) => g.category);
    setExpandedCategories(new Set(allCategories));
  }, [groupedPermissions]);

  const toggleCategory = (category: string) => {
    setExpandedCategories(prev => {
      const newSet = new Set(prev);
      if (newSet.has(category)) {
        newSet.delete(category);
      } else {
        newSet.add(category);
      }
      return newSet;
    });
  };

  const handlePermissionToggle = (permissionCode: string) => {
    if (disabled) return;

    const newSelected = selectedPermissionCodes.includes(permissionCode)
      ? selectedPermissionCodes.filter(code => code !== permissionCode)
      : [...selectedPermissionCodes, permissionCode];

    onChange(newSelected);
  };

  const handleCategoryToggle = (category: GroupedPermissionResponseDto) => {
    if (disabled) return;

    const categoryCodes = category.permissions.map((p: PermissionResponseDto) => p.code);
    const allSelected = categoryCodes.every((code: string) => selectedPermissionCodes.includes(code));

    let newSelected: string[];
    if (allSelected) {
      // Deselect all in this category
      newSelected = selectedPermissionCodes.filter((code: string) => !categoryCodes.includes(code));
    } else {
      // Select all in this category
      const missingCodes = categoryCodes.filter((code: string) => !selectedPermissionCodes.includes(code));
      newSelected = [...selectedPermissionCodes, ...missingCodes];
    }

    onChange(newSelected);
  };

  const isCategoryFullySelected = (category: GroupedPermissionResponseDto): boolean => {
    return category.permissions.every((p: PermissionResponseDto) => selectedPermissionCodes.includes(p.code));
  };

  const isCategoryPartiallySelected = (category: GroupedPermissionResponseDto): boolean => {
    const selected = category.permissions.filter((p: PermissionResponseDto) => selectedPermissionCodes.includes(p.code));
    return selected.length > 0 && selected.length < category.permissions.length;
  };

  if (groupedPermissions.length === 0) {
    return (
      <div className="text-center py-8 text-gray-500">
        <p>No hay permisos disponibles</p>
      </div>
    );
  }

  return (
    <div className="space-y-2 max-h-96 overflow-y-auto border border-gray-200 rounded-lg p-4 bg-gray-50">
      {groupedPermissions.map((group) => {
        const isExpanded = expandedCategories.has(group.category);
        const isFullySelected = isCategoryFullySelected(group);
        const isPartiallySelected = isCategoryPartiallySelected(group);

        return (
          <div key={group.category} className="bg-white rounded-lg border border-gray-200 shadow-sm">
            {/* Category Header */}
            <div className="flex items-center p-3 border-b border-gray-100">
              <button
                type="button"
                onClick={() => toggleCategory(group.category)}
                className="flex items-center flex-1 text-left"
              >
                <ChevronRightIcon
                  className={`w-4 h-4 mr-2 text-gray-500 transition-transform ${isExpanded ? 'rotate-90' : ''}`}
                />
                <span className="font-semibold text-gray-700 capitalize">
                  {group.category}
                </span>
                <span className="ml-2 text-xs text-gray-500">
                  ({group.permissions.length} permisos)
                </span>
              </button>

              {/* Select All Category Checkbox */}
              <div className="flex items-center">
                <input
                  type="checkbox"
                  id={`category-all-${group.category}`}
                  checked={isFullySelected}
                  ref={input => {
                    if (input) {
                      input.indeterminate = isPartiallySelected;
                    }
                  }}
                  onChange={() => handleCategoryToggle(group)}
                  disabled={disabled}
                  className="w-4 h-4 text-green-600 border-gray-300 rounded focus:ring-green-500 disabled:opacity-50"
                  title="Seleccionar todos"
                />
                <label htmlFor={`category-all-${group.category}`} className="ml-2 text-xs text-gray-500">
                  Todos
                </label>
              </div>
            </div>

            {/* Permissions List */}
            {isExpanded && (
              <div className="p-3 space-y-2">
                {group.permissions.map((permission) => (
                  <div
                    key={permission.code}
                    className="flex items-start p-2 hover:bg-gray-50 rounded transition-colors"
                  >
                    <input
                      type="checkbox"
                      id={permission.code}
                      checked={selectedPermissionCodes.includes(permission.code)}
                      onChange={() => handlePermissionToggle(permission.code)}
                      disabled={disabled}
                      className="w-4 h-4 mt-0.5 text-green-600 border-gray-300 rounded focus:ring-green-500 disabled:opacity-50"
                    />
                    <label
                      htmlFor={permission.code}
                      className="ml-3 flex-1 cursor-pointer"
                    >
                      <div className="text-sm font-medium text-gray-700">
                        {permission.name}
                      </div>
                      <div className="text-xs text-gray-500 font-mono">
                        {permission.code}
                      </div>
                      {permission.description && (
                        <div className="text-xs text-gray-500 mt-1">
                          {permission.description}
                        </div>
                      )}
                    </label>
                  </div>
                ))}
              </div>
            )}
          </div>
        );
      })}
    </div>
  );
});
