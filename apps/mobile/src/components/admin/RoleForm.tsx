/**
 * RoleForm — BottomSheetForm para crear/editar rol.
 * Campos: código, nombre + árbol de permisos con checkboxes.
 */
import React, { useState, useEffect } from 'react';
import { View, StyleSheet, TouchableOpacity } from 'react-native';
import { Input } from '../atoms/Input';
import { Button } from '../atoms/Button';
import { Checkbox } from '../atoms/Checkbox';
import { Text } from '../atoms/Text';
import { Icon } from '../atoms/Icon';
import { Alert } from '../molecules/Alert';
import { BottomSheetForm } from '../organisms/BottomSheetForm';
import type {
  RoleResponseDto,
  CreateRoleRequestDto,
  UpdateRoleRequestDto,
  GroupedPermissionResponseDto,
} from '@hydroespinaca/shared';
import { adminService, spacing, semanticColors, colors, borderRadius, typography } from '@hydroespinaca/shared';
import { showToast } from '../../utils/toast';
import { hapticSuccess, hapticError } from '../../utils/haptics';

export interface RoleFormProps {
  isOpen: boolean;
  onClose: () => void;
  role?: RoleResponseDto | null;
  groupedPermissions: GroupedPermissionResponseDto[];
  onSuccess: () => void;
}

export function RoleForm({
  isOpen,
  onClose,
  role,
  groupedPermissions,
  onSuccess,
}: RoleFormProps): React.ReactElement {
  const isEdit = !!role;

  const [code, setCode] = useState('');
  const [name, setName] = useState('');
  const [selectedPerms, setSelectedPerms] = useState<Set<string>>(new Set());
  const [expandedCategories, setExpandedCategories] = useState<Set<string>>(new Set());
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (isOpen) {
      if (role) {
        setCode(role.code);
        setName(role.name);
        setSelectedPerms(new Set(role.permissionCodes ?? []));
      } else {
        setCode('role_');
        setName('');
        setSelectedPerms(new Set());
      }
      // Expand all categories by default
      setExpandedCategories(new Set(groupedPermissions.map((g) => g.category)));
      setError(null);
    }
  }, [isOpen, role, groupedPermissions]);

  const togglePerm = (permCode: string) => {
    setSelectedPerms((prev) => {
      const next = new Set(prev);
      if (next.has(permCode)) {
        next.delete(permCode);
      } else {
        next.add(permCode);
      }
      return next;
    });
  };

  const toggleCategory = (category: string) => {
    const group = groupedPermissions.find((g) => g.category === category);
    if (!group) return;

    const allCodes = group.permissions.map((p) => p.code);
    const allSelected = allCodes.every((c) => selectedPerms.has(c));

    setSelectedPerms((prev) => {
      const next = new Set(prev);
      if (allSelected) {
        allCodes.forEach((c) => next.delete(c));
      } else {
        allCodes.forEach((c) => next.add(c));
      }
      return next;
    });
  };

  const toggleExpand = (category: string) => {
    setExpandedCategories((prev) => {
      const next = new Set(prev);
      if (next.has(category)) {
        next.delete(category);
      } else {
        next.add(category);
      }
      return next;
    });
  };

  const validate = (): string | null => {
    if (!code.trim() || code.trim().length < 5) return 'El código debe tener al menos 5 caracteres';
    if (code.length > 100) return 'Máximo 100 caracteres para el código';
    if (!name.trim() || name.trim().length < 2) return 'El nombre debe tener al menos 2 caracteres';
    if (name.length > 100) return 'Máximo 100 caracteres para el nombre';
    return null;
  };

  const handleSubmit = async () => {
    const validationError = validate();
    if (validationError) {
      setError(validationError);
      return;
    }

    setLoading(true);
    setError(null);

    try {
      if (isEdit && role) {
        const dto: UpdateRoleRequestDto = {
          name: name.trim(),
          permissionCodes: Array.from(selectedPerms),
        };
        await adminService.updateRole(role.code, dto);
      } else {
        const dto: CreateRoleRequestDto = {
          code: code.trim(),
          name: name.trim(),
          permissionCodes: Array.from(selectedPerms),
        };
        await adminService.createRole(dto);
      }
      showToast('success', isEdit ? 'Rol actualizado' : 'Rol creado');
      hapticSuccess();
      onSuccess();
      onClose();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Error al guardar rol');
      showToast('error', err instanceof Error ? err.message : 'Error al guardar rol');
      hapticError();
    } finally {
      setLoading(false);
    }
  };

  return (
    <BottomSheetForm
      title={isEdit ? 'Editar Rol' : 'Nuevo Rol'}
      isOpen={isOpen}
      onClose={onClose}
      snapPoints={['75%', '95%']}
    >
      <View style={styles.form}>
        {error && <Alert type="error" message={error} onDismiss={() => setError(null)} />}

        <View style={styles.field}>
          <Text variant="label" color={semanticColors.textSecondary} style={styles.label}>
            Código
          </Text>
          <Input
            value={code}
            onChangeText={setCode}
            placeholder="role_nombre"
            editable={!isEdit}
            autoCapitalize="none"
            autoCorrect={false}
            maxLength={100}
          />
          {isEdit && (
            <Text variant="caption" color={semanticColors.textTertiary}>
              El código no se puede cambiar
            </Text>
          )}
        </View>

        <View style={styles.field}>
          <Text variant="label" color={semanticColors.textSecondary} style={styles.label}>
            Nombre
          </Text>
          <Input
            value={name}
            onChangeText={setName}
            placeholder="Nombre del rol"
            maxLength={100}
          />
        </View>

        {/* Permissions tree */}
        <View style={styles.field}>
          <Text variant="label" color={semanticColors.textSecondary} style={styles.label}>
            Permisos ({selectedPerms.size} seleccionado{selectedPerms.size !== 1 ? 's' : ''})
          </Text>

          {groupedPermissions.map((group) => {
            const isExpanded = expandedCategories.has(group.category);
            const allCodes = group.permissions.map((p) => p.code);
            const selectedCount = allCodes.filter((c) => selectedPerms.has(c)).length;
            const allSelected = selectedCount === allCodes.length && allCodes.length > 0;
            const someSelected = selectedCount > 0 && !allSelected;

            return (
              <View key={group.category} style={styles.categoryContainer}>
                {/* Category header */}
                <TouchableOpacity
                  style={styles.categoryHeader}
                  onPress={() => toggleExpand(group.category)}
                  activeOpacity={0.7}
                >
                  <Icon
                    name={isExpanded ? 'chevron-down' : 'chevron-right'}
                    size={18}
                    color={semanticColors.textSecondary}
                  />
                  <Checkbox
                    checked={allSelected}
                    onToggle={() => toggleCategory(group.category)}
                  />
                  <Text
                    variant="body"
                    color={semanticColors.textPrimary}
                    style={styles.categoryName}
                  >
                    {group.category}
                  </Text>
                  <Text variant="caption" color={semanticColors.textTertiary}>
                    {selectedCount}/{allCodes.length}
                  </Text>
                </TouchableOpacity>

                {/* Permissions */}
                {isExpanded && (
                  <View style={styles.permissionsList}>
                    {group.permissions.map((perm) => (
                      <View key={perm.code} style={styles.permissionItem}>
                        <Checkbox
                          checked={selectedPerms.has(perm.code)}
                          onToggle={() => togglePerm(perm.code)}
                          label={perm.name}
                        />
                        {perm.description && (
                          <Text
                            variant="caption"
                            color={semanticColors.textTertiary}
                            style={styles.permDescription}
                          >
                            {perm.description}
                          </Text>
                        )}
                      </View>
                    ))}
                  </View>
                )}
              </View>
            );
          })}
        </View>

        <Button
          variant="primary"
          onPress={handleSubmit}
          loading={loading}
          fullWidth
          style={styles.submitButton}
        >
          {isEdit ? 'Guardar Cambios' : 'Crear Rol'}
        </Button>
      </View>
    </BottomSheetForm>
  );
}

const styles = StyleSheet.create({
  form: {
    gap: spacing.md,
  },
  field: {
    gap: spacing.xs,
  },
  label: {
    fontWeight: typography.fontWeight.medium,
  },
  // Permission tree
  categoryContainer: {
    backgroundColor: colors.gray[50],
    borderRadius: borderRadius.md,
    borderWidth: 1,
    borderColor: colors.gray[200],
    overflow: 'hidden',
    marginTop: spacing.xs,
  },
  categoryHeader: {
    flexDirection: 'row',
    alignItems: 'center',
    padding: spacing.sm,
    gap: spacing.sm,
    backgroundColor: colors.gray[100],
  },
  categoryName: {
    flex: 1,
    fontWeight: typography.fontWeight.semibold,
  },
  permissionsList: {
    padding: spacing.sm,
    paddingLeft: spacing.lg,
    gap: spacing.sm,
  },
  permissionItem: {
    gap: 2,
  },
  permDescription: {
    marginLeft: 30,
  },
  submitButton: {
    marginTop: spacing.sm,
  },
});
