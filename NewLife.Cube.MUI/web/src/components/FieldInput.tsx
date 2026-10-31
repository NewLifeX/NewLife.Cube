/**
 * 字段渲染组件（MUI 皮肤）
 *
 * 控件类型一律由共享规则引擎 `resolveWidget` 决定，本组件只把抽象 widget 映射到 MUI 控件：
 * 与 Vue/Svelte/Angular 皮肤共用同一套 ItemType 口径（密码/颜色/时长/地区/映射下拉/多选/日期…）。
 */
import { useEffect, useRef, useState, type ReactNode } from 'react';
import {
  TextField, Switch, Select, MenuItem, FormControl, FormControlLabel,
  InputLabel, Typography, Stack, Box, type TextFieldProps,
} from '@mui/material';
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
  label?: string;
  size?: 'small' | 'medium';
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

export default function FieldInput({ mapping, value, onChange, label, size = 'small' }: FieldInputProps): ReactNode {
  const { widget, field, props: wprops } = mapping;
  const text = label ?? field.displayName ?? field.name;
  const commonProps: Partial<TextFieldProps> = {
    label: text,
    size,
    fullWidth: true,
    required: field.required,
    disabled: field.readOnly,
  };

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
  const areaValueRef = useRef(value);

  useEffect(() => {
    if (widget !== 'area') return;
    cascadeRef.current ??= createAreaCascade(api.area, areaLevel);
    let cancelled = false;
    void cascadeRef.current.init(Number(value ?? 0) || 0).then((state) => {
      if (cancelled) return;
      setAreaOptions(state.options);
      setAreaPath(state.path);
      areaValueRef.current = state.value;
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
    areaValueRef.current = state.value;
    onChange(state.value ?? '');
  };

  switch (widget) {
    case 'area':
      return (
        <Stack spacing={1} sx={{ width: '100%' }}>
          {areaOptions.map((levelOptions, level) => (
            <FormControl key={level} fullWidth size={size}>
              <InputLabel shrink>{level === 0 ? `${text}（省）` : '下级地区'}</InputLabel>
              <Select
                notched
                displayEmpty
                value={String(areaPath[level] ?? '')}
                label={level === 0 ? `${text}（省）` : '下级地区'}
                onChange={(e) => void onAreaChange(level, e.target.value)}
              >
                <MenuItem value="">-- 请选择 --</MenuItem>
                {levelOptions.map((opt) => (
                  <MenuItem key={opt.id} value={String(opt.id)}>{opt.name}</MenuItem>
                ))}
              </Select>
            </FormControl>
          ))}
        </Stack>
      );

    case 'timeSpan':
      return (
        <Stack direction="row" spacing={1} sx={{ width: '100%' }}>
          {([
            { key: 'hours' as const, label: '时', max: undefined, step: 1, p: spanParts.hours },
            { key: 'minutes' as const, label: '分', max: 59, step: 1, p: spanParts.minutes },
            { key: 'seconds' as const, label: '秒', max: 59, step: spanUnit === 'milliseconds' ? 0.001 : 1, p: spanParts.seconds },
          ]).map((part) => (
            <TextField
              key={part.key}
              type="number"
              size={size}
              label={part.label}
              value={part.p}
              onChange={(e) => onSpanChange(part.key, e.target.value)}
              slotProps={{ htmlInput: { min: 0, max: part.max, step: part.step } }}
            />
          ))}
        </Stack>
      );

    case 'switch':
      return (
        <FormControlLabel
          control={<Switch checked={!!value} onChange={(_, v) => onChange(v)} disabled={field.readOnly} />}
          label={text}
        />
      );

    case 'select':
    case 'lov':
    case 'lovMulti':
    case 'mapSelect': {
      const multiple = widget === 'lovMulti' || !!wprops?.multiple;
      return (
        <FormControl fullWidth size={size}>
          <InputLabel shrink>{text}</InputLabel>
          <Select
            multiple={multiple}
            notched
            displayEmpty
            value={multiple ? toMultiArray(value) : (value ?? '')}
            label={text}
            onChange={(e) => {
              const raw = e.target.value as string | string[];
              if (!multiple) {
                onChange(raw ?? '');
                return;
              }
              const array = Array.isArray(raw) ? raw : toMultiArray(raw);
              onChange(array.join(','));
            }}
          >
            {!multiple && <MenuItem value="">-- 请选择 --</MenuItem>}
            {optionList.map(([k, v]) => (
              <MenuItem key={k} value={k}>{v}</MenuItem>
            ))}
          </Select>
        </FormControl>
      );
    }

    case 'number':
    case 'mapInput':
      return (
        <TextField
          {...commonProps}
          type="number"
          value={value ?? ''}
          onChange={(e) => onChange(e.target.value)}
          slotProps={{ htmlInput: { step: wprops?.precision ? 10 ** -(Number(wprops.precision) || 0) : 1 } }}
        />
      );

    case 'datetime':
      return (
        <TextField {...commonProps} type="datetime-local" slotProps={{ inputLabel: { shrink: true } }} value={value ?? ''} onChange={(e) => onChange(e.target.value)} />
      );

    case 'date':
      return (
        <TextField {...commonProps} type="date" slotProps={{ inputLabel: { shrink: true } }} value={value ?? ''} onChange={(e) => onChange(e.target.value)} />
      );

    case 'time':
      return (
        <TextField {...commonProps} type="time" slotProps={{ inputLabel: { shrink: true } }} value={value ?? ''} onChange={(e) => onChange(e.target.value)} />
      );

    case 'textarea':
      return (
        <TextField {...commonProps} multiline rows={4} value={value ?? ''} onChange={(e) => onChange(e.target.value)} />
      );

    case 'password':
      return (
        <TextField {...commonProps} type="password" autoComplete="new-password" value={value ?? ''} onChange={(e) => onChange(e.target.value)} />
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
      return (
        <TextField {...commonProps} type="url" value={value ?? ''} onChange={(e) => onChange(e.target.value)} />
      );

    case 'color':
      return (
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, width: '100%' }}>
          <Box sx={{ width: 24, height: 24, borderRadius: 1, border: '1px solid rgba(0,0,0,0.24)', bgcolor: value || '#000000' }} />
          <TextField {...commonProps} value={value ?? ''} placeholder="请输入颜色值，如 #1976d2" onChange={(e) => onChange(e.target.value)} />
        </Box>
      );

    case 'image':
    case 'file':
      // 上传由业务页处理，这里只维护附件地址（与 MVC 一致：字段值即路径）
      return (
        <TextField {...commonProps} value={value ?? ''} placeholder={widget === 'image' ? '图片地址' : '附件地址'} onChange={(e) => onChange(e.target.value)} />
      );

    case 'code':
    case 'html':
    case 'markdown':
    case 'json':
      return (
        <TextField {...commonProps} multiline rows={8} value={value ?? ''} onChange={(e) => onChange(e.target.value)}
          slotProps={{ input: { sx: { fontFamily: 'monospace' } } }}
        />
      );

    case 'readonly':
      return (
        <Box sx={{ width: '100%' }}>
          <Typography variant="caption" color="text.secondary">{text}</Typography>
          <Typography variant="body2">{value ?? '-'}</Typography>
        </Box>
      );

    case 'link':
      return (
        <TextField {...commonProps} type="url" value={value ?? ''} onChange={(e) => onChange(e.target.value)} />
      );

    default:
      return (
        <TextField {...commonProps} value={value ?? ''} onChange={(e) => onChange(e.target.value)} />
      );
  }
}
