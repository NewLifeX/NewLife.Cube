import type { Component } from 'vue';
import type { WidgetProvider } from '@cube/api-core';

export interface WidgetDefinition {
  kind: string;
  title: string;
  providers: WidgetProvider[];
  defaultW: 3 | 4 | 6 | 12;
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
