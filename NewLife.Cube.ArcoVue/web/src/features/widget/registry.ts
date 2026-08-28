import type { Component } from 'vue';
import type { WidgetProvider, WidgetWidth } from '@cube/api-core';

export interface WidgetDefinition {
  kind: string;
  title: string;
  providers: WidgetProvider[];
  defaultW: WidgetWidth;
  component: Component;
}

const registry = new Map<string, WidgetDefinition>();

export function registerWidget(def: WidgetDefinition) {
  registry.set(def.kind, def);
}

export function getWidget(kind: string): WidgetDefinition | undefined {
  return registry.get(kind);
}

export function listWidgets(): WidgetDefinition[] {
  return [...registry.values()];
}
