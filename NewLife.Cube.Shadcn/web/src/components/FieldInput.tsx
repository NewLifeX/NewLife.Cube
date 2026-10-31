/**
 * 字段渲染组件（Shadcn 皮肤）
 *
 * 控件类型一律由共享规则引擎 `resolveWidget` 决定，本组件只把抽象 widget 映射到 shadcn/ui 控件：
 * 与 Vue/Svelte/Angular 皮肤共用同一套 ItemType 口径（密码/颜色/时长/地区/映射下拉/多选/日期…）。
 */
import { useEffect, useRef, useState, type ReactNode } from 'react';
import { Input } from '@/components/ui/input';
import { Textarea } from '@/components/ui/textarea';
import { Switch } from '@/components/ui/switch';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import type { AreaOption, FieldMapping, TimeSpanParts, TimeSpanUnit } from '@newlifex/cube';
import {
  createAreaCascade,
  joinTimeSpan,
  splitTimeSpan,
  timeSpanUnitOfDescription,
} from '@newlifex/cube';
import api from '@/api';

interface FieldInputProps {
  mapping: FieldMapping;
  value: any;
  onChange: (value: any) => void;
}

/** 多选值归一：数组 / 逗号串 / JSON 串 */
function toMultiArray(v: any): string[] {
  if (v == null || v === '') return [];
  if (Array.isArray(v)) return v.map(String);
  const text = String(v).trim();
  if (!text) return [];
  try {
    const parsed = JSON.parse(text);
    if (Array.isArray(parsed)) return parsed.map(String);
  } catch {
    /* 非 JSON，按逗号串处理 */
  }
  return text.split(',').filter(Boolean);
}

export default function FieldInput({ mapping, value, onChange }: FieldInputProps): ReactNode {
  const { widget, field, props: wprops } = mapping;
  const label = field.displayName ?? field.name;
  const dataSource = (wprops?.dataSource ?? field.dataSource) as Record<string, string> | undefined;
  const optionList = dataSource ? Object.entries(dataSource) : [];

  // ── 时长 ──
  const spanUnit = (wprops?.unit as TimeSpanUnit) ?? timeSpanUnitOfDescription(field.description);
  const [spanParts, setSpanParts] = useState<TimeSpanParts>(() => splitTimeSpan(value, spanUnit));
  const spanRef = useRef(value);
  useEffect(() => {
    if (spanRef.current === value) return;
    spanRef.current = value;
    setSpanParts(splitTimeSpan(value, spanUnit));
  }, [value, spanUnit]);

  const onSpanChange = (key: keyof TimeSpanParts, v: string) => {
    const next = { ...spanParts, [key]: Number(v || 0) };
    setSpanParts(next);
    const joined = joinTimeSpan(next, spanUnit);
    spanRef.current = joined;
    onChange(joined);
  };

  // ── 地区级联 ──
  const areaLevel = Number(wprops?.areaLevel ?? 3) || 3;
  const [areaOptions, setAreaOptions] = useState<AreaOption[][]>([]);
  const [areaPath, setAreaPath] = useState<number[]>([]);
  const cascadeRef = useRef<ReturnType<typeof createAreaCascade> | null>(null);

  useEffect(() => {
    if (widget !== 'area') return;
    cascadeRef.current ??= createAreaCascade(api.area, areaLevel);
    let cancelled = false;
    void cascadeRef.current.init(Number(value ?? 0) || 0).then((state) => {
      if (cancelled) return;
      setAreaOptions(state.options);
      setAreaPath(state.path);
    });
    return () => {
      cancelled = true;
    };
    // 仅初次挂载/切换字段时初始化，后续由 onAreaChange 维护
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [widget, areaLevel, field.name]);

  const onAreaChange = async (level: number, raw: string) => {
    if (!cascadeRef.current) return;
    const state = await cascadeRef.current.selectLevel(level, raw === '' ? null : Number(raw));
    setAreaOptions(state.options);
    setAreaPath(state.path);
    onChange(state.value ?? '');
  };

  if (widget === 'area') {
    return (
      <div className="space-y-1">
        <Label>{label}</Label>
        {areaOptions.map((levelOptions, level) => (
          <select
            key={level}
            className="flex h-9 w-full rounded-md border border-input bg-transparent px-3 py-1 text-sm shadow-sm focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring"
            value={String(areaPath[level] ?? '')}
            onChange={(e) => void onAreaChange(level, e.target.value)}
          >
            <option value="">{level === 0 ? '请选择省份' : '请选择下级'}</option>
            {levelOptions.map((opt) => (
              <option key={opt.id} value={String(opt.id)}>{opt.name}</option>
            ))}
          </select>
        ))}
      </div>
    );
  }

  if (widget === 'timeSpan') {
    return (
      <div className="space-y-1">
        <Label>{label}</Label>
        <div className="flex items-center gap-2">
          {([
            { key: 'hours' as const, hint: '时', max: undefined, step: 1, p: spanParts.hours },
            { key: 'minutes' as const, hint: '分', max: 59, step: 1, p: spanParts.minutes },
            { key: 'seconds' as const, hint: '秒', max: 59, step: spanUnit === 'milliseconds' ? 0.001 : 1, p: spanParts.seconds },
          ]).map((part) => (
            <Input
              key={part.key}
              type="number"
              min={0}
              max={part.max}
              step={part.step}
              placeholder={part.hint}
              value={part.p}
              onChange={(e) => onSpanChange(part.key, e.target.value)}
            />
          ))}
        </div>
      </div>
    );
  }

  switch (widget) {
    case 'switch':
      return (
        <div className="flex items-center gap-2">
          <Switch checked={!!value} onCheckedChange={(v) => onChange(v)} disabled={field.readOnly} id={field.name} />
          <Label htmlFor={field.name}>{label}</Label>
        </div>
      );

    case 'select':
    case 'lov':
    case 'lovMulti':
    case 'mapSelect': {
      const multiple = widget === 'lovMulti' || !!wprops?.multiple;
      if (multiple) {
        // 多选走原生 multiple select（shadcn 无多选原语），提交逗号串
        const picked = toMultiArray(value);
        return (
          <div className="space-y-1">
            <Label>{label}</Label>
            <select
              multiple
              size={Math.min(6, Math.max(2, optionList.length))}
              className="w-full rounded-md border border-input bg-transparent px-3 py-1 text-sm shadow-sm"
              value={picked}
              onChange={(e) => {
                const next = Array.from(e.target.selectedOptions).map((o) => o.value);
                onChange(next.join(','));
              }}
            >
              {optionList.map(([k, v]) => (
                <option key={k} value={k}>{v}</option>
              ))}
            </select>
          </div>
        );
      }
      return (
        <div className="space-y-1">
          <Label>{label}</Label>
          <Select value={String(value ?? '')} onValueChange={onChange} disabled={field.readOnly}>
            <SelectTrigger><SelectValue placeholder="请选择" /></SelectTrigger>
            <SelectContent>
              {optionList.map(([k, v]) => (
                <SelectItem key={k} value={k}>{v}</SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
      );
    }

    case 'number':
    case 'mapInput':
      return (
        <div className="space-y-1">
          <Label>{label}</Label>
          <Input
            type="number"
            step={wprops?.precision ? 10 ** -(Number(wprops.precision) || 0) : 1}
            value={value ?? ''}
            onChange={(e) => onChange(e.target.value)}
            disabled={field.readOnly}
            required={field.required}
          />
        </div>
      );

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

    case 'time':
      return (
        <div className="space-y-1">
          <Label>{label}</Label>
          <Input type="time" value={value ?? ''} onChange={(e) => onChange(e.target.value)} disabled={field.readOnly} />
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
          <Input type="password" autoComplete="new-password" value={value ?? ''} onChange={(e) => onChange(e.target.value)} disabled={field.readOnly} />
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
          <div className="flex items-center gap-2">
            <span className="h-5 w-5 rounded border" style={{ background: value || '#000000' }} />
            <Input value={value ?? ''} placeholder="请输入颜色值，如 #0f172a" onChange={(e) => onChange(e.target.value)} disabled={field.readOnly} />
          </div>
        </div>
      );

    case 'image':
    case 'file':
      // 上传由业务页处理，这里只维护附件地址（与 MVC 一致：字段值即路径）
      return (
        <div className="space-y-1">
          <Label>{label}</Label>
          <Input
            value={value ?? ''}
            placeholder={widget === 'image' ? '图片地址' : '附件地址'}
            onChange={(e) => onChange(e.target.value)}
            disabled={field.readOnly}
          />
        </div>
      );

    case 'code':
    case 'html':
    case 'markdown':
    case 'json':
      return (
        <div className="space-y-1">
          <Label>{label}</Label>
          <Textarea rows={8} className="font-mono text-xs" value={value ?? ''} onChange={(e) => onChange(e.target.value)} disabled={field.readOnly} />
        </div>
      );

    case 'readonly':
      return (
        <div className="space-y-1">
          <Label>{label}</Label>
          <p className="text-sm text-muted-foreground">{value ?? '-'}</p>
        </div>
      );

    default:
      return (
        <div className="space-y-1">
          <Label>{label}</Label>
          <Input
            type={widget === 'icon' ? 'text' : 'text'}
            value={value ?? ''}
            onChange={(e) => onChange(e.target.value)}
            disabled={field.readOnly}
            required={field.required}
          />
        </div>
      );
  }
}
