import React, { useState } from 'react';
import { View, ScrollView } from 'react-native';
import { 
  Text,
  Heading,
  Label,
  Button,
  Icon,
  Input,
  Select,
  TextArea,
  Checkbox,
  Radio,
  Switch,
  Slider,
  Divider,
  Avatar,
  Badge,
  FloatingActionButton,
  IconButton,
  Pressable,
  ProgressBar,
  Spinner,
  StatusIndicator,
} from '../../components/atoms';
import { ScreenLayout } from '../../components/templates';
import { spacing, semanticColors, typography, borderRadius, HomeIcon, SettingsIcon, UserIcon, CloseIcon, MenuIcon, SearchIcon, PlusIcon, MinusIcon, CheckIcon, ArrowLeftIcon, ArrowRightIcon, HeartIcon, StarIcon, BellIcon, MailIcon, PhoneIcon, CameraIcon, EditIcon, DeleteIcon, SaveIcon, RefreshIcon, DownloadIcon, UploadIcon, LockIcon, UnlockIcon, EyeIcon, EyeOffIcon, CalendarIcon, ClockIcon, LocationIcon, WifiIcon, BatteryIcon, PowerIcon, WarningIcon, InfoIcon, ErrorIcon, SuccessIcon } from '@hydroespinaca/shared';
import { 
  Alert, 
  Card, 
  FormField, 
  SearchBar, 
  ListItem,
  TabBar,
  Stat,
  RangeInput,
  Pagination,
  Toast,
  useToast,
  EmptyState,
  EmptyStateNoData,
  EmptyStateNoResults,
  DatePicker,
  DatePickerBirthday,
  DatePickerAppointment,
  DatePickerDeadline,
  DatePickerRange,
  TimePicker,
  TimePickerComponent,
  TimePickerAlarm,
  Breadcrumb,
  BreadcrumbNavigation,
  BreadcrumbMinimal
} from '../../components/molecules';
import { DataList, ReadingsTable } from '../../components/organisms';
import { useReadingsStore } from '@hydroespinaca/shared';
import { type TimeValue } from '../../components/molecules/TimePicker';

// Sección estable fuera del componente para evitar remounts en cada render
const Section = ({ title, children }: { title: string; children: React.ReactNode }) => (
  <View style={{ marginBottom: spacing.xl }}>
    <Heading level={4} style={{ marginBottom: spacing.md }}>{title}</Heading>
    {children}
  </View>
);

// Componente demo para DataList y ReadingsTable
const DataListDemo = () => {
  const {
    sensorSummary,
    individualReadings,
    initializeReadings
  } = useReadingsStore();
  const [activeMode, setActiveMode] = useState<'summary' | 'individual'>('summary');

  // Inicializar datos al montar el componente
  React.useEffect(() => {
      initializeReadings();
    }, [initializeReadings]);

  const handleRefresh = () => {
      initializeReadings();
    };

  const handleSummaryItemPress = (item: any) => {
    console.log('Summary item pressed:', item);
  };

  const handleIndividualItemPress = (item: any) => {
    console.log('Individual item pressed:', item);
  };

  return (
    <Section title="Organisms - DataList & ReadingsTable">
      <View style={{ gap: spacing.md }}>
        {/* Selector de modo */}
        <View style={{ flexDirection: 'row', gap: spacing.sm }}>
          <Button
            variant={activeMode === 'summary' ? 'primary' : 'outline'}
            onPress={() => setActiveMode('summary')}
            style={{ flex: 1 }}
          >
            Resumen de Sensores
          </Button>
          <Button
            variant={activeMode === 'individual' ? 'primary' : 'outline'}
            onPress={() => setActiveMode('individual')}
            style={{ flex: 1 }}
          >
            Lecturas Individuales
          </Button>
        </View>

        {/* ReadingsTable con datos reales */}
        <View style={{ height: 400, backgroundColor: semanticColors.surface, borderRadius: borderRadius.md }}>
          <ReadingsTable
            mode={activeMode}
            summaryData={sensorSummary}
            individualData={individualReadings}
            loading={false}
            error={null}
            onRefresh={handleRefresh}
            refreshing={false}
            onSummaryItemPress={handleSummaryItemPress}
            onIndividualItemPress={handleIndividualItemPress}
            showSensorIcons={true}
            dateFormat="short"
            emptyMessage={
              activeMode === 'summary' 
                ? 'No hay datos de resumen disponibles' 
                : 'No hay lecturas individuales disponibles'
            }
          />
        </View>

        {/* Ejemplo de DataList básico con datos simples */}
        <Text variant="h4" style={{ marginTop: spacing.lg, marginBottom: spacing.sm }}>
          DataList Básico (Ejemplo)
        </Text>
        <View style={{ height: 200, backgroundColor: semanticColors.surface, borderRadius: borderRadius.md }}>
          <DataList
            data={[
              { id: '1', title: 'Item 1', description: 'Descripción del item 1' },
              { id: '2', title: 'Item 2', description: 'Descripción del item 2' },
              { id: '3', title: 'Item 3', description: 'Descripción del item 3' },
            ]}
            renderItem={({ item }) => (
              <View style={{ 
                padding: spacing.md, 
                backgroundColor: semanticColors.background,
                marginHorizontal: spacing.md,
                marginVertical: spacing.xs,
                borderRadius: borderRadius.sm,
                borderWidth: 1,
                borderColor: semanticColors.borderMuted
              }}>
                <Text variant="bodyLarge" style={{ fontWeight: 'bold', marginBottom: spacing.xs }}>
                  {item.title}
                </Text>
                <Text variant="bodySmall" color={semanticColors.textSecondary}>
                  {item.description}
                </Text>
              </View>
            )}
            title="Lista de Ejemplo"
            subtitle="Ejemplo básico de DataList con datos estáticos"
            emptyMessage="No hay items para mostrar"
          />
        </View>
      </View>
    </Section>
  );
};

export function SandboxScreen(): React.ReactElement {
  // Estados para componentes interactivos
  const [radioValue, setRadioValue] = useState('uno');
  const [switchOn, setSwitchOn] = useState(false);
  const [switchOnDisabled, setSwitchOnDisabled] = useState(true);
  const [checkboxChecked, setCheckboxChecked] = useState(false);
  const [checkboxIndeterminate, setCheckboxIndeterminate] = useState(true);
  const [inputValue, setInputValue] = useState('');
  const [inputValueFilled, setInputValueFilled] = useState('Valor inicial');
  const [textAreaValue, setTextAreaValue] = useState('Texto de ejemplo para comprobar alto y contador.');
  const [selectValue, setSelectValue] = useState<string | number>('2');
  const [errorSelectValue, setErrorSelectValue] = useState<string | number>('2');
  const [disabledSelectValue] = useState<string | number>('2');
  const [sliderValue, setSliderValue] = useState(40);
  const [searchValue1, setSearchValue1] = useState('');
  const [searchValue2, setSearchValue2] = useState('');
  const [formValue, setFormValue] = useState('');
  const [nameValue, setNameValue] = useState('');
  const [emailValue, setEmailValue] = useState('');
  
  // Estados para nuevas moléculas
  const [activeTab, setActiveTab] = useState('1');
  const [rangeValue, setRangeValue] = useState<[number, number]>([20, 80]);
  const [currentPage, setCurrentPage] = useState(1);
  
  // Estados separados para cada DatePicker
  const [generalDate, setGeneralDate] = useState<Date>(new Date());
  const [birthdayDate, setBirthdayDate] = useState<Date>(new Date());
  const [appointmentDate, setAppointmentDate] = useState<Date>(new Date());
  const [deadlineDate, setDeadlineDate] = useState<Date>(new Date());
  
  const [selectedTime, setSelectedTime] = useState<TimeValue | undefined>();
  const [startDate, setStartDate] = useState<Date>(new Date());
  const [endDate, setEndDate] = useState<Date>(new Date(Date.now() + 7 * 24 * 60 * 60 * 1000)); // 7 días después
  
  // Hook para Toast
  const { show: showToast, hide: hideToast, ToastComponent } = useToast();

  const selectOptions = [
    { label: 'Opción 1', value: '1' },
    { label: 'Opción 2', value: '2' },
    { label: 'Opción 3 (deshabilitada)', value: '3', disabled: true },
    { label: 'Opción 4', value: '4' },
  ];

  return (
    <ScreenLayout>
      <ScrollView contentContainerStyle={{ padding: 24, paddingBottom: 48 }}>
        {/* Tipografía */}
        <Section title="Tipografía">
          <Heading level={1}>Heading 1</Heading>
          <Heading level={2}>Heading 2</Heading>
          <Heading level={3}>Heading 3</Heading>
          <Heading level={4}>Heading 4</Heading>
          <Heading level={5}>Heading 5</Heading>
          <Heading level={6}>Heading 6</Heading>

          <Text variant="body" style={{ marginTop: spacing.md }}>
            Texto body por defecto con lineHeight normalizado.
          </Text>
          <Text variant="bodyLarge">Texto bodyLarge</Text>
          <Text variant="bodySmall">Texto bodySmall</Text>
          <Text variant="label">Texto label</Text>
          <Text variant="caption">Texto caption</Text>
        </Section>

        <Divider style={{ marginVertical: spacing.md }} />

        {/* Inputs */}
        <Section title="Inputs">
          <Label>Outlined (md)</Label>
          <Input
            placeholder="Escribe algo..."
            value={inputValue}
            onChangeText={setInputValue}
            variant="outlined"
            size="md"
            leftIcon={<Text>🔍</Text>}
            rightIcon={<Text>⏎</Text>}
            style={{}}
          />

          <View style={{ height: spacing.md }} />

          <Label>Filled (lg) con valor</Label>
          <Input
            placeholder="Placeholder"
            value={inputValueFilled}
            onChangeText={setInputValueFilled}
            variant="filled"
            size="lg"
          />

          <View style={{ height: spacing.md }} />

          <Label>Error (sm) y Underlined</Label>
          <Input
            placeholder="Con error"
            value={inputValue}
            onChangeText={setInputValue}
            variant="underlined"
            size="sm"
            error
          />

          <View style={{ height: spacing.md }} />

          <Label>Disabled</Label>
          <Input placeholder="Deshabilitado" disabled />

          <View style={{ height: spacing.lg }} />

          <Label>TextArea outlined con contador</Label>
          <TextArea
            value={textAreaValue}
            onChangeText={setTextAreaValue}
            rows={4}
            maxLength={140}
            showCharacterCount
          />

          <View style={{ height: spacing.md }} />

          <Label>TextArea filled (disabled)</Label>
          <TextArea
            value={"Línea 1\nLínea 2"}
            variant="filled"
            disabled
          />
        </Section>

        <Divider style={{ marginVertical: spacing.md }} />

        {/* Select */}
        <Section title="Select">
          <Label>Normal</Label>
          <Select
            options={selectOptions}
            value={selectValue}
            onSelect={(opt) => setSelectValue(opt.value)}
          />

          <View style={{ height: spacing.md }} />

          <Label>Error</Label>
          <Select
            options={selectOptions}
            value={errorSelectValue}
            onSelect={(opt) => setErrorSelectValue(opt.value)}
            error
          />

          <View style={{ height: spacing.md }} />

          <Label>Disabled</Label>
          <Select
            options={selectOptions}
            value={disabledSelectValue}
            onSelect={() => {}}
            disabled
          />
        </Section>

        <Divider style={{ marginVertical: spacing.md }} />

        {/* Controles de selección */}
        <Section title="Checkbox, Radio y Switch">
          <Label>Checkboxes</Label>
          <View style={{ flexDirection: 'row', alignItems: 'center' }}>
            <Checkbox
              checked={checkboxChecked}
              onPress={setCheckboxChecked}
              label="Normal"
              style={{ marginRight: spacing.lg }}
            />
            <Checkbox
              indeterminate={checkboxIndeterminate}
              onPress={() => setCheckboxIndeterminate((v) => !v)}
              label="Indeterminado"
              style={{ marginRight: spacing.lg }}
            />
            <Checkbox label="Disabled" disabled />
          </View>

          <View style={{ height: spacing.md }} />

          <Label>Radios</Label>
          <View>
            <Radio
              selected={radioValue === 'uno'}
              onPress={() => setRadioValue('uno')}
              label="Opción UNO (derecha)"
              value="uno"
              style={{ marginBottom: 8 }}
            />
            <Radio
              selected={radioValue === 'dos'}
              onPress={() => setRadioValue('dos')}
              label="Opción DOS (izquierda)"
              labelPosition="left"
              value="dos"
            />
          </View>

          <View style={{ height: spacing.md }} />

          <Label>Switches</Label>
          <View style={{ flexDirection: 'row', alignItems: 'center' }}>
            <Switch
              value={switchOn}
              onValueChange={setSwitchOn}
              label="Activo"
              style={{ marginRight: spacing.lg }}
            />
            <Switch
              value={switchOnDisabled}
              onValueChange={setSwitchOnDisabled}
              label="Disabled"
              disabled
            />
          </View>
        </Section>

        <Divider style={{ marginVertical: spacing.md }} />

        {/* Slider */}
        <Section title="Slider">
          <Slider
            value={sliderValue}
            onValueChange={setSliderValue}
            onSlidingComplete={setSliderValue}
            minimumValue={0}
            maximumValue={100}
            step={5}
            showValue
          />
        </Section>

        <Divider style={{ marginVertical: spacing.md }} />

        {/* Otros (Button, Icon, Divider) */}
        <Section title="Otros átomos">
          <Label>Button</Label>
          <View style={{ flexDirection: 'row', alignItems: 'center', gap: spacing.sm }}>
            <Button>Primary</Button>
            <Button variant="secondary">Secondary</Button>
            <Button variant="outline">Outline</Button>
          </View>
          
          <View style={{ height: spacing.sm }} />
          <Label>Icon</Label>
          <View style={{ flexDirection: 'row', alignItems: 'center', gap: spacing.sm }}>
            <Icon name="home" />
            <Icon name="user" />
            <Icon name="settings" />
            <Icon name="search" />
            <Icon name="heart" />
          </View>

          <View style={{ height: spacing.md }} />
        </Section>

        {/* Avatar */}
        <Section title="Avatar">
          <View style={{ flexDirection: 'row', alignItems: 'center', gap: spacing.md }}>
            <Avatar fallback="JP" size="sm" backgroundColor={semanticColors.primary} textColor={semanticColors.textInverse} />
            <Avatar fallback="MG" size="md" backgroundColor={semanticColors.errorBg} textColor={semanticColors.errorText} />
            <Avatar fallback="CL" size="lg" backgroundColor={semanticColors.successBg} textColor={semanticColors.successText} />
            <Avatar fallback="AM" size={88} backgroundColor={semanticColors.infoBg} textColor={semanticColors.infoText} />
          </View>
        </Section>

        {/* Badge */}
        <Section title="Badge">
          <View style={{ flexDirection: 'row', alignItems: 'center', gap: spacing.sm, flexWrap: 'wrap' }}>
            <Badge>Default</Badge>
            <Badge variant="primary">Primary</Badge>
            <Badge variant="success">Success</Badge>
            <Badge variant="warning">Warning</Badge>
            <Badge variant="error">Error</Badge>
            <Badge variant="info">Info</Badge>
          </View>
        </Section>

        {/* IconButton */}
        {/* Nuevo Sistema de Iconos SVG */}
        <Section title="Iconos SVG Unificados">
          <View style={{ gap: spacing.md }}>
            <Text variant="bodySmall" color={semanticColors.textSecondary}>
              Sistema de iconos SVG que funciona tanto en web como en móvil
            </Text>
            
            {/* Iconos individuales */}
            <View style={{ flexDirection: 'row', flexWrap: 'wrap', gap: spacing.md }}>
              <HomeIcon size={24} color={semanticColors.primary} />
              <SettingsIcon size={24} color={semanticColors.textPrimary} />
              <UserIcon size={24} color={semanticColors.successText} />
              <CloseIcon size={24} color={semanticColors.errorText} />
              <MenuIcon size={24} color={semanticColors.warningText} />
              <SearchIcon size={24} color={semanticColors.infoText} />
              <PlusIcon size={24} color={semanticColors.primary} />
              <MinusIcon size={24} color={semanticColors.textSecondary} />
              <CheckIcon size={24} color={semanticColors.successText} />
              <ArrowLeftIcon size={24} color={semanticColors.textPrimary} />
              <ArrowRightIcon size={24} color={semanticColors.textPrimary} />
              <HeartIcon size={24} color={semanticColors.errorText} />
              <StarIcon size={24} color={semanticColors.warningText} />
              <BellIcon size={24} color={semanticColors.infoText} />
              <MailIcon size={24} color={semanticColors.primary} />
              <PhoneIcon size={24} color={semanticColors.successText} />
              <CameraIcon size={24} color={semanticColors.textPrimary} />
              <EditIcon size={24} color={semanticColors.primary} />
              <DeleteIcon size={24} color={semanticColors.errorText} />
              <SaveIcon size={24} color={semanticColors.successText} />
              <RefreshIcon size={24} color={semanticColors.infoText} />
              <DownloadIcon size={24} color={semanticColors.primary} />
              <UploadIcon size={24} color={semanticColors.warningText} />
              <LockIcon size={24} color={semanticColors.errorText} />
              <UnlockIcon size={24} color={semanticColors.successText} />
              <EyeIcon size={24} color={semanticColors.textPrimary} />
              <EyeOffIcon size={24} color={semanticColors.textSecondary} />
              <CalendarIcon size={24} color={semanticColors.primary} />
              <ClockIcon size={24} color={semanticColors.infoText} />
              <LocationIcon size={24} color={semanticColors.errorText} />
              <WifiIcon size={24} color={semanticColors.successText} />
              <BatteryIcon size={24} color={semanticColors.warningText} />
              <PowerIcon size={24} color={semanticColors.primary} />
              <WarningIcon size={24} color={semanticColors.warningText} />
              <InfoIcon size={24} color={semanticColors.infoText} />
              <ErrorIcon size={24} color={semanticColors.errorText} />
              <SuccessIcon size={24} color={semanticColors.successText} />
            </View>
            
            {/* Diferentes tamaños */}
            <View style={{ flexDirection: 'row', alignItems: 'center', gap: spacing.md }}>
              <HomeIcon size={16} color={semanticColors.textSecondary} />
              <HomeIcon size={20} color={semanticColors.textPrimary} />
              <HomeIcon size={24} color={semanticColors.primary} />
              <HomeIcon size={32} color={semanticColors.successText} />
              <HomeIcon size={40} color={semanticColors.warningText} />
            </View>
          </View>
        </Section>

        <Section title="IconButton (Nuevo Sistema)">
          <View style={{ flexDirection: 'row', alignItems: 'center', gap: spacing.sm }}>
            <IconButton icon="settings" size="sm" />
            <IconButton icon="home" size="md" variant="primary" />
            <IconButton icon="user" size="lg" variant="secondary" />
            <IconButton icon="heart" variant="outline" />
            <IconButton icon="star" variant="ghost" />
            <IconButton icon="close" variant="primary" color={semanticColors.backgroundPrimary} />
          </View>
        </Section>

        {/* Pressable */}
        <Section title="Pressable">
          <Pressable style={{ padding: spacing.md, backgroundColor: semanticColors.backgroundSecondary, borderRadius: 8 }}>
            <Text>Pressable Component - Toca aquí</Text>
          </Pressable>
        </Section>

        {/* ProgressBar */}
        <Section title="ProgressBar">
          <Label>Progress: 65%</Label>
          <ProgressBar progress={65} showLabel />
          <View style={{ height: spacing.sm }} />
          <Label>Progress: 30% (Custom Color)</Label>
          <ProgressBar progress={30} color={semanticColors.warningBg} showLabel />
        </Section>

        {/* Spinner */}
        <Section title="Spinner">
          <View style={{ flexDirection: 'row', alignItems: 'center', gap: spacing.md, padding: spacing.md, backgroundColor: semanticColors.backgroundSecondary, borderRadius: 8 }}>
            <Spinner size="sm" color={semanticColors.primary} />
            <Spinner size="md" color={semanticColors.errorText} />
            <Spinner size="lg" color={semanticColors.successText} />
            <Spinner size={40} color={semanticColors.infoText} />
          </View>
        </Section>

        {/* StatusIndicator */}
        <Section title="StatusIndicator">
          <View style={{ 
            padding: spacing.md, 
            backgroundColor: semanticColors.backgroundSecondary, 
            borderRadius: 8,
            gap: spacing.md 
          }}>
            <View style={{ flexDirection: 'row', alignItems: 'center', gap: spacing.md }}>
              <StatusIndicator status="online" size="sm" />
              <StatusIndicator status="warning" size="md" />
              <StatusIndicator status="error" size="lg" />
              <StatusIndicator status="offline" size={20} />
            </View>
            <View style={{ flexDirection: 'row', alignItems: 'center', gap: spacing.md }}>
              <StatusIndicator status="loading" animated size="md" />
              <StatusIndicator status="success" size="lg" />
              <StatusIndicator status="online" animated size="lg" />
            </View>
          </View>
        </Section>

        {/* Divider */}
        <Section title="Divider">
          <Text>Texto antes del divider</Text>
          <Divider style={{ marginVertical: spacing.sm }} />
          <Text>Texto después del divider</Text>
        </Section>
        {/* Molecules - Alert */}
        <Section title="Molecules - Alert">
          <View style={{ gap: spacing.sm }}>
            <Alert type="info" title="Información" message="Este es un mensaje informativo." closable />
            <Alert type="success" title="Éxito" message="Operación realizada correctamente." closable />
            <Alert type="warning" title="Advertencia" message="Revisa los datos ingresados." closable />
            <Alert type="error" title="Error" message="Ha ocurrido un error inesperado." closable />
          </View>
        </Section>

        {/* Molecules - Card */}
        <Section title="Molecules - Card">
          <View style={{ flexDirection: 'row', gap: spacing.md }}>
            <Card elevated style={{ flex: 1 }}>
              <Text weight="medium">Card Elevada</Text>
              <Text color={semanticColors.textSecondary}>Contenido dentro de una card elevada.</Text>
            </Card>
            <Card style={{ flex: 1 }}>
              <Text weight="medium">Card Plano</Text>
              <Text color={semanticColors.textSecondary}>Contenido dentro de una card plana.</Text>
            </Card>
          </View>
        </Section>

        {/* Molecules - FormField */}
        <Section title="Molecules - FormField">
          <View style={{ gap: spacing.md }}>
            <FormField
              label="Nombre"
              required
              value={nameValue}
              onChangeText={setNameValue}
              placeholder="Ingresa tu nombre"
            />
            <FormField
              label="Correo electrónico"
              errorText="El correo no es válido"
              value={emailValue}
              onChangeText={setEmailValue}
              placeholder="ejemplo@correo.com"
            />
          </View>
        </Section>

        {/* Molecules - SearchBar */}
        <Section title="Molecules - SearchBar">
          <View style={{ gap: spacing.md }}>
            <SearchBar
              value={searchValue1}
              onChangeText={setSearchValue1}
              onClear={() => setSearchValue1('')}
              onSubmit={(text) => console.log('Buscar:', text)}
              placeholder="Buscar productos"
            />
            <SearchBar
              value={searchValue2}
              onChangeText={setSearchValue2}
              onClear={() => setSearchValue2('')}
              onSubmit={(text) => console.log('Buscar:', text)}
              placeholder="Buscar en catálogo"
              variant="filled"
              showCancel
              onCancel={() => {
                setSearchValue2('');
                console.log('Cancelar');
              }}
            />
          </View>
        </Section>

        {/* Molecules - ListItem */}
        <Section title="Molecules - ListItem">
          <View style={{ backgroundColor: semanticColors.backgroundPrimary, borderRadius: borderRadius.md }}>
            <ListItem
              title="Espinaca"
              subtitle="Verdura fresca"
              left={<Icon name="star" />}
              right={<Badge>Nuevo</Badge>}
              onPress={() => console.log('Espinaca')}
            />
            <ListItem
              title="Tomate"
              subtitle="Fruta roja"
              dense
              left={<Icon name="star" />}
              right={<Text weight="medium">$2.99</Text>}
            />
            <ListItem
              title="Zanahoria"
              subtitle="Raíz anaranjada"
              divider={false}
              left={<Icon name="star" />}
              right={<Badge variant="primary">Oferta</Badge>}
            />
          </View>
        </Section>

        {/* Molecules - TabBar */}
        <Section title="Molecules - TabBar">
          <TabBar
            tabs={[
              { key: '1', title: 'Inicio', icon: 'home' },
              { key: '2', title: 'Productos', icon: 'menu' },
              { key: '3', title: 'Perfil', icon: 'user' },
              { key: '4', title: 'Configuración', icon: 'settings' }
            ]}
            activeTab={activeTab}
            onTabPress={setActiveTab}
          />
        </Section>

        {/* Molecules - Stat */}
        <Section title="Molecules - Stat">
          <View style={{ flexDirection: 'row', gap: spacing.md }}>
            <Stat
              label="Ventas"
              value="$12,345"
              trend={{ value: "+12.5%", direction: 1, description: "vs mes anterior" }}
              icon="arrow-right"
            />
            <Stat
              label="Usuarios"
              value="1,234"
              trend={{ value: "-5.2%", direction: -1, description: "vs mes anterior" }}
              icon="user"
            />
          </View>
          <View style={{ marginTop: spacing.md }}>
            <Stat
              label="Productos"
              value="456"
              description="Total en inventario"
              variant="card"
              size="large"
            />
          </View>
        </Section>

        {/* Molecules - RangeInput */}
        <Section title="Molecules - RangeInput">
          <RangeInput
            label="Rango de precios"
            values={rangeValue}
            onValuesChange={setRangeValue}
            minimumValue={0}
            maximumValue={100}
            step={5}
            showValues
            valuePrefix="$"
          />
        </Section>

        {/* Molecules - Pagination */}
        <Section title="Molecules - Pagination">
          <Pagination
            currentPage={currentPage}
            totalPages={10}
            onPageChange={setCurrentPage}
            showFirstLast
          />
        </Section>

        {/* Molecules - DatePicker */}
        <Section title="Molecules - DatePicker">
          <View style={{ gap: spacing.md }}>
            <DatePicker
              label="Fecha general"
              value={generalDate}
              onDateChange={setGeneralDate}
              placeholder="Seleccionar fecha"
            />
            <DatePickerBirthday
              value={birthdayDate}
              onDateChange={setBirthdayDate}
            />
            <DatePickerAppointment
              value={appointmentDate}
              onDateChange={setAppointmentDate}
            />
            <DatePickerDeadline
              value={deadlineDate}
              onDateChange={setDeadlineDate}
            />
            <DatePickerRange
              startDate={startDate}
              endDate={endDate}
              onStartDateChange={setStartDate}
              onEndDateChange={setEndDate}
            />
          </View>
        </Section>

        {/* Molecules - TimePicker */}
        <Section title="Molecules - TimePicker">
          <View style={{ gap: spacing.md }}>
            <TimePickerComponent
              label="Hora general"
              value={selectedTime ?? { hours: 0, minutes: 0, seconds: 0 }}
              onTimeChange={(timeValue: TimeValue) => {
                setSelectedTime(timeValue);
              }}
              placeholder="Seleccionar hora"
            />
            <TimePickerComponent
              label="Hora específica"
              value={{ hours: 14, minutes: 30, seconds: 0 }}
              onTimeChange={(time) => console.log('Time:', time)}
            />
            <TimePickerAlarm
              value={{ hours: 7, minutes: 0, seconds: 0 }}
              onTimeChange={(time) => console.log('Alarm:', time)}
            />
          </View>
        </Section>

        {/* Molecules - Breadcrumb */}
        <Section title="Molecules - Breadcrumb">
          <View style={{ gap: spacing.md }}>
            <Breadcrumb
              items={[
                { id: '1', label: 'Inicio', path: '/' },
                { id: '2', label: 'Productos', path: '/productos' },
                { id: '3', label: 'Categoría', path: '/productos/categoria' },
                { id: '4', label: 'Producto', isActive: true }
              ]}
              onItemPress={(item, index) => console.log('Breadcrumb:', item, index)}
            />
            <BreadcrumbNavigation
              items={[
                { id: '1', label: 'Dashboard' },
                { id: '2', label: 'Configuración' },
                { id: '3', label: 'Usuario', isActive: true }
              ]}
            />
            <BreadcrumbMinimal
              items={[
                { id: '1', label: 'Home' },
                { id: '2', label: 'Docs' },
                { id: '3', label: 'API', isActive: true }
              ]}
            />
          </View>
        </Section>

        {/* Molecules - EmptyState */}
        <Section title="Molecules - EmptyState">
          <View style={{ gap: spacing.lg }}>
            <EmptyState
              title="No hay datos"
              description="No se encontraron elementos para mostrar"
              iconName="search"
              enableButton
              buttonText="Agregar elemento"
              onButtonPress={() => console.log('Add item')}
            />
            <EmptyStateNoData />
            <EmptyStateNoResults />
          </View>
        </Section>

        {/* Organisms - DataList & ReadingsTable */}
        <DataListDemo />

        {/* Molecules - Toast */}
        <Section title="Molecules - Toast">
          <View style={{ gap: spacing.md }}>
            <Button
              onPress={() => showToast({ text1: 'Mensaje de éxito', type: 'success' })}
              variant="primary"
            >
              Mostrar Toast Éxito
            </Button>
            <Button
              onPress={() => showToast({ text1: 'Mensaje de error', type: 'error' })}
              variant="secondary"
            >
              Mostrar Toast Error
            </Button>
            <Button
              onPress={() => showToast({ text1: 'Mensaje informativo', type: 'info' })}
              variant="outline"
            >
              Mostrar Toast Info
            </Button>
          </View>
        </Section>

        {/* Toast Component */}
        <ToastComponent />
      </ScrollView>
    </ScreenLayout>
  );
}