import { InjectionKey } from "vue";
import { ProvidePage } from "../model";

const providePageKey = Symbol() as InjectionKey<ProvidePage>

export default providePageKey;