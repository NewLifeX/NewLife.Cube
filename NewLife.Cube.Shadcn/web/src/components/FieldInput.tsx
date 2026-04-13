import { type ReactNode } from 'react';
import { Input } from '@/components/ui/input';
import { Textarea } from '@/components/ui/textarea';
import { Switch } from '@/components/ui/switch';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import type { FieldMapping } from '@cube/field-mapping';

interface FieldInputProps {
  mapping: FieldMapping;
  value: any;
  onChange: (value: any) => void;
}

export default function FieldInput({ mapping, value, onChange }: FieldInputProps): ReactNode {
  const { widget, field } = mapping;
  const label = field.displayName ?? field.name;

  switch (widget) {
    case 'number':
      return (
        <div className="space-y-1">
          <Label>{label}</Label>
          <Input type="number" value={value ?? ''} onChange={(e) => onChange(e.target.value)} disabled={field.readOnly} required={field.required} />
        </div>
      );

    case 'switch':
      return (
        <div className="flex items-center gap-2">
          <Switch checked={!!value} onCheckedChange={(v) => onChange(v)} disabled={field.readOnly} id={field.name} />
          <Label htmlFor={field.name}>{label}</Label>
        </div>
      );

    case 'select': {
      const ds = field.dataSource ?? {};
      return (
        <div className="space-y-1">
          <Label>{label}</Label>
          <Select value={String(value ?? '')} onValueChange={onChange} disabled={field.readOnly}>
            <SelectTrigger><SelectValue placeholder="请选择" /></SelectTrigger>
            <SelectContent>
              {Object.entries(ds).map(([k, v]) => (
                <SelectItem key={k} value={k}>{v}</SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
      );
    }

    case 'datetime':
      return (
        <div className="space-y-1">
          <Label>{label}</Label>
          <Input type="datetime-local" value={value ?? ''} onChange={(e) => onChange(e.target.value)} disabled={field.readOnly} />
        </div>
      );

    case 'date':
      return (
        <div className="space-y-1">
          <Label>{label}</Label>
          <Input type="date" value={value ?? ''} onChange={(e) => onChange(e.target.value)} disabled={field.readOnly} />
        </div>
      );

    case 'textarea':
      return (
        <div className="space-y-1">
          <Label>{label}</Label>
          <Textarea rows={4} value={value ?? ''} onChange={(e) => onChange(e.target.value)} disabled={field.readOnly} required={field.required} />
        </div>
      );

    case 'password':
      return (
        <div className="space-y-1">
          <Label>{label}</Label>
          <Input type="password" value={value ?? ''} onChange={(e) => onChange(e.target.value)} disabled={field.readOnly} />
        </div>
      );

    case 'email':
      return (
        <div className="space-y-1">
          <Label>{label}</Label>
          <Input type="email" value={value ?? ''} onChange={(e) => onChange(e.target.value)} disabled={field.readOnly} />
        </div>
      );

    case 'phone':
      return (
        <div className="space-y-1">
          <Label>{label}</Label>
          <Input type="tel" value={value ?? ''} onChange={(e) => onChange(e.target.value)} disabled={field.readOnly} />
        </div>
      );

    case 'url':
    case 'link':
      return (
        <div className="space-y-1">
          <Label>{label}</Label>
          <Input type="url" value={value ?? ''} onChange={(e) => onChange(e.target.value)} disabled={field.readOnly} />
        </div>
      );

    case 'color':
      return (
        <div className="space-y-1">
          <Label>{label}</Label>
          <Input type="color" value={value ?? '#000000'} onChange={(e) => onChange(e.target.value)} className="h-9 w-16 p-1" />
        </div>
      );

    case 'image':
      return (
        <div className="space-y-1">
          <Label>{label}</Label>
          <Input type="file" accept="image/*" onChange={(e: any) => onChange(e.target.files?.[0])} disabled={field.readOnly} />
        </div>
      );

    case 'file':
      return (
        <div className="space-y-1">
          <Label>{label}</Label>
          <Input type="file" onChange={(e: any) => onChange(e.target.files?.[0])} disabled={field.readOnly} />
        </div>
      );

    case 'code':
    case 'html':
      return (
        <div className="space-y-1">
          <Label>{label}</Label>
          <Textarea rows={8} className="font-mono text-xs" value={value ?? ''} onChange={(e) => onChange(e.target.value)} disabled={field.readOnly} />
        </div>
      );

    default:
      return (
        <div className="space-y-1">
          <Label>{label}</Label>
          <Input value={value ?? ''} onChange={(e) => onChange(e.target.value)} disabled={field.readOnly} required={field.required} />
        </div>
      );
  }
}
