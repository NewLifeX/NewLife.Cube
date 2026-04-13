import { type ReactNode } from 'react';
import {
  TextField, Switch, Select, MenuItem, FormControl, FormControlLabel,
  InputLabel, type TextFieldProps,
} from '@mui/material';
import type { FieldMapping } from '@cube/field-mapping';

interface FieldInputProps {
  mapping: FieldMapping;
  value: any;
  onChange: (value: any) => void;
  label?: string;
  size?: 'small' | 'medium';
}

export default function FieldInput({ mapping, value, onChange, label, size = 'small' }: FieldInputProps): ReactNode {
  const { widget, field } = mapping;
  const commonProps: Partial<TextFieldProps> = {
    label: label ?? field.displayName ?? field.name,
    size,
    fullWidth: true,
    required: field.required,
    disabled: field.readOnly,
  };

  switch (widget) {
    case 'number':
      return (
        <TextField {...commonProps} type="number" value={value ?? ''} onChange={(e) => onChange(e.target.value)} />
      );

    case 'switch':
      return (
        <FormControlLabel
          control={<Switch checked={!!value} onChange={(_, v) => onChange(v)} disabled={field.readOnly} />}
          label={label ?? field.displayName ?? field.name}
        />
      );

    case 'select': {
      const ds = field.dataSource ?? {};
      return (
        <FormControl fullWidth size={size}>
          <InputLabel>{label ?? field.displayName ?? field.name}</InputLabel>
          <Select value={value ?? ''} label={commonProps.label as string} onChange={(e) => onChange(e.target.value)}>
            <MenuItem value="">-- 请选择 --</MenuItem>
            {Object.entries(ds).map(([k, v]) => (
              <MenuItem key={k} value={k}>{v}</MenuItem>
            ))}
          </Select>
        </FormControl>
      );
    }

    case 'datetime':
      return (
        <TextField {...commonProps} type="datetime-local" slotProps={{ inputLabel: { shrink: true } }} value={value ?? ''} onChange={(e) => onChange(e.target.value)} />
      );

    case 'date':
      return (
        <TextField {...commonProps} type="date" slotProps={{ inputLabel: { shrink: true } }} value={value ?? ''} onChange={(e) => onChange(e.target.value)} />
      );

    case 'textarea':
      return (
        <TextField {...commonProps} multiline rows={4} value={value ?? ''} onChange={(e) => onChange(e.target.value)} />
      );

    case 'password':
      return (
        <TextField {...commonProps} type="password" value={value ?? ''} onChange={(e) => onChange(e.target.value)} />
      );

    case 'email':
      return (
        <TextField {...commonProps} type="email" value={value ?? ''} onChange={(e) => onChange(e.target.value)} />
      );

    case 'phone':
      return (
        <TextField {...commonProps} type="tel" value={value ?? ''} onChange={(e) => onChange(e.target.value)} />
      );

    case 'url':
    case 'link':
      return (
        <TextField {...commonProps} type="url" value={value ?? ''} onChange={(e) => onChange(e.target.value)} />
      );

    case 'color':
      return (
        <TextField {...commonProps} type="color" value={value ?? '#000000'} onChange={(e) => onChange(e.target.value)} />
      );

    case 'image':
      return (
        <TextField {...commonProps} type="file" slotProps={{ htmlInput: { accept: 'image/*' }, inputLabel: { shrink: true } }} onChange={(e: any) => onChange(e.target.files?.[0])} />
      );

    case 'file':
      return (
        <TextField {...commonProps} type="file" slotProps={{ inputLabel: { shrink: true } }} onChange={(e: any) => onChange(e.target.files?.[0])} />
      );

    case 'code':
    case 'html':
      return (
        <TextField {...commonProps} multiline rows={8} value={value ?? ''} onChange={(e) => onChange(e.target.value)}
          slotProps={{ input: { sx: { fontFamily: 'monospace' } } }}
        />
      );

    default:
      return (
        <TextField {...commonProps} value={value ?? ''} onChange={(e) => onChange(e.target.value)} />
      );
  }
}
