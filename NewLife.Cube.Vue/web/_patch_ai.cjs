const fs = require('fs');
const file = 'C:/X/NewLife.Cube/NewLife.Cube.Vue/web/src/components/page/index.vue';
let text = fs.readFileSync(file, 'utf8');

// 1. Insert drawer before </template> (the last closing tag before <script>)
text = text.replace('\n</template>\n\n<script setup lang="ts">', '\n  <!-- AI Drawer -->\n  <AiInsightDrawer v-model="insightVisible" :url="insightUrl" :thinking="insightThinking" />\n</template>\n\n<script setup lang="ts">');

// 2. Add import after echarts
text = text.replace("import * as echarts from 'echarts';", "import * as echarts from 'echarts';\nimport AiInsightDrawer from '../table/AiInsightDrawer.vue';");

fs.writeFileSync(file, text, 'utf8');
console.log('Patch applied');
