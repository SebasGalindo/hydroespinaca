/**
 * UserForm — BottomSheetForm para crear/editar usuario.
 * Campos: username, email, password, rol.
 */
import React, { useState, useEffect } from 'react';
import { View, StyleSheet } from 'react-native';
import { Input } from '../atoms/Input';
import { Button } from '../atoms/Button';
import { Select } from '../molecules/Select';
import { Text } from '../atoms/Text';
import { Alert } from '../molecules/Alert';
import { BottomSheetForm } from '../organisms/BottomSheetForm';
import type {
  UserResponseDto,
  UserCreateDto,
  UserUpdateDto,
  RoleResponseDto,
} from '@hydroespinaca/shared';
import { adminService, spacing, semanticColors, typography } from '@hydroespinaca/shared';
import { showToast } from '../../utils/toast';
import { hapticSuccess, hapticError } from '../../utils/haptics';

export interface UserFormProps {
  isOpen: boolean;
  onClose: () => void;
  user?: UserResponseDto | null;
  roles: RoleResponseDto[];
  onSuccess: () => void;
}

export function UserForm({
  isOpen,
  onClose,
  user,
  roles,
  onSuccess,
}: UserFormProps): React.ReactElement {
  const isEdit = !!user;

  const [username, setUsername] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [roleId, setRoleId] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (isOpen) {
      if (user) {
        setUsername(user.username);
        setEmail(user.email);
        setPassword('');
        setRoleId(user.roleId ?? '');
      } else {
        setUsername('');
        setEmail('');
        setPassword('');
        setRoleId('');
      }
      setError(null);
    }
  }, [isOpen, user]);

  const roleOptions = roles.map((r) => ({ label: r.name, value: r.id }));

  const validate = (): string | null => {
    if (!username.trim()) return 'El nombre de usuario es requerido';
    if (username.length > 100) return 'Máximo 100 caracteres para el nombre';
    if (!email.trim()) return 'El email es requerido';
    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) return 'Formato de email inválido';
    if (!isEdit && !password) return 'La contraseña es requerida';
    if (password && password.length < 8) return 'La contraseña debe tener al menos 8 caracteres';
    if (password && password.length > 128) return 'Máximo 128 caracteres para la contraseña';
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
      if (isEdit && user) {
        const dto: UserUpdateDto = {};
        if (username !== user.username) dto.username = username;
        if (email !== user.email) dto.email = email;
        if (password) dto.password = password;
        if (roleId !== (user.roleId ?? '')) dto.roleId = roleId || null;
        await adminService.updateUser(user.id, dto);
      } else {
        const dto: UserCreateDto = {
          username: username.trim(),
          email: email.trim(),
          password,
          roleId: roleId || null,
        };
        await adminService.createUser(dto);
      }
      showToast('success', isEdit ? 'Usuario actualizado' : 'Usuario creado');
      hapticSuccess();
      onSuccess();
      onClose();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Error al guardar usuario');
      showToast('error', err instanceof Error ? err.message : 'Error al guardar usuario');
      hapticError();
    } finally {
      setLoading(false);
    }
  };

  return (
    <BottomSheetForm
      title={isEdit ? 'Editar Usuario' : 'Nuevo Usuario'}
      isOpen={isOpen}
      onClose={onClose}
      snapPoints={['70%', '90%']}
    >
      <View style={styles.form}>
        {error && <Alert type="error" message={error} onDismiss={() => setError(null)} />}

        <View style={styles.field}>
          <Text variant="label" color={semanticColors.textSecondary} style={styles.label}>
            Nombre de usuario
          </Text>
          <Input
            value={username}
            onChangeText={setUsername}
            placeholder="Nombre de usuario"
            autoCapitalize="none"
            autoCorrect={false}
            maxLength={100}
          />
        </View>

        <View style={styles.field}>
          <Text variant="label" color={semanticColors.textSecondary} style={styles.label}>
            Email
          </Text>
          <Input
            value={email}
            onChangeText={setEmail}
            placeholder="correo@ejemplo.com"
            keyboardType="email-address"
            autoCapitalize="none"
            autoCorrect={false}
          />
        </View>

        <View style={styles.field}>
          <Text variant="label" color={semanticColors.textSecondary} style={styles.label}>
            {isEdit ? 'Nueva contraseña (opcional)' : 'Contraseña'}
          </Text>
          <Input
            value={password}
            onChangeText={setPassword}
            placeholder={isEdit ? 'Dejar vacío para mantener' : 'Mínimo 8 caracteres'}
            secureTextEntry
            autoCapitalize="none"
            autoCorrect={false}
          />
        </View>

        <View style={styles.field}>
          <Text variant="label" color={semanticColors.textSecondary} style={styles.label}>
            Rol
          </Text>
          <Select
            options={[{ label: 'Sin rol', value: '' }, ...roleOptions]}
            value={roleId}
            onValueChange={setRoleId}
            placeholder="Seleccionar rol"
          />
        </View>

        <Button
          variant="primary"
          onPress={handleSubmit}
          loading={loading}
          fullWidth
          style={styles.submitButton}
        >
          {isEdit ? 'Guardar Cambios' : 'Crear Usuario'}
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
  submitButton: {
    marginTop: spacing.sm,
  },
});
