<template>
  <div>
    <div class="table-responsive">
      <table
        class="table table-bordered table-hover table-striped table-condensed"
      >
        <thead>
          <tr>
            <th colspan="4">
              服务器信息
            </th>
          </tr>
        </thead>
        <tbody>
          <tr>
            <td class="name">
              应用系统：
            </td>
            <td class="value">
              <!-- @if (this.has((PermissionFlags)16)) -->

              <a style="cursor: pointer;" @click="restart">重启应用系统</a>

              &nbsp;&nbsp;&nbsp;&nbsp;{{ model.rawUrl }}
            </td>
            <td class="name">
              目录：
            </td>
            <td class="value">
              {{ model.contentRootPath }}
            </td>
          </tr>
          <tr>
            <td class="name">
              域名地址：
            </td>
            <td class="value">
              <span title="主机">{{ model.host }}</span
              >，
              <span title="本地">{{ model.localHost }}</span>
              &nbsp;<span title="远程">{{ model.remoteHost }}</span>
            </td>
            <td class="name">
              应用程序：
            </td>
            <td class="value">
              <span :title="model.commandLine">{{ model.processName }}</span>
            </td>
          </tr>
          <tr>
            <td class="name">
              应用域：
            </td>
            <td class="value">
              {{ model.curDomainFriendlyName }}
              <!-- <a href="@Url.action("Main", new{ id = "Assembly" })" target="_blank" title="点击打开进程程序集列表">程序集列表</a>
                    <a href="@Url.action("Main", new{ id = "ProcessModules" })" target="_blank" title="点击打开进程模块列表">模块列表</a>
                    <a href="@Url.action("Main", new{ id = "ServerVar" })" target="_blank" title="点击打开服务器变量列表">服务器变量列表</a> -->
            </td>
            <td class="name">
              .net 版本：
            </td>
            <td class="value">
              {{ model.envVersion }} &nbsp;{{ model.frameworkName }}
            </td>
          </tr>
          <tr>
            <td class="name">
              操作系统：
            </td>
            <td class="value" :title="model.guid">
              {{ model.oSName }} {{ model.oSVersion }}
            </td>
            <td class="name">
              机器用户：
            </td>
            <td class="value" :title="model.uuid">
              <span v-if="model.product !== undefined"
                >{{ model.product }}，</span
              >
              {{ model.userName + '/' + model.machineName }}
            </td>
          </tr>
          <tr>
            <td class="name">
              处理器：
            </td>
            <td class="value" :title="model.cpuID">
              {{ model.processor }}，
              {{ model.processorCount }}
              核心，{{ model.cpuRate }}
              <span v-if="model.temperature > 0"
                >，{{ model.temperature }} ℃</span
              >
            </td>
            <td class="name">
              时间：
            </td>

            <td
              class="value"
              title="这里使用了服务器默认的时间格式！后面是开机时间。"
            >
              {{ model.dateTimeNow }}，开机{{ model.uptime }}
            </td>
          </tr>
          <tr>
            <td class="name">
              内存：
            </td>
            <td class="value">
              物理：{{ model.availableMemory }}M / {{ model.memory }}M，
              工作/提交: {{ model.workingSet64 }}M/{{
                model.privateMemorySize64
              }}M GC: {{ model.totalMemory }}M
              <a @click="memoryFree" title="点击释放进程内存">释放内存</a>
            </td>
            <td class="name">
              进程时间：
            </td>
            <td class="value">
              <!-- @process.totalProcessorTime.totalSeconds.toString("N2")秒 启动于 @process.startTime.toLocalTime().toFullString() -->
            </td>
          </tr>
          <tr>
            <td class="name">
              Session：
            </td>
            <td class="value">
              <!-- @httpContext.session.keys.count() 个 -->
              <a
                @click="main('Session')"
                target="_blank"
                title="点击打开Session列表"
                >Session列表</a
              >
              <!-- ，@gc -->
            </td>
            <td class="name">
              <!-- @{ var app = ApplicationManager.load();} -->
              应用启动：
            </td>
            <td class="value">
              <!-- 启动于 @app.startTime.toLocalTime().toFullString() -->
            </td>
          </tr>
        </tbody>
      </table>
      <table
        class="table table-bordered table-hover table-striped table-condensed"
      >
        <thead>
          <tr>
            <th>名称</th>
            <th>标题</th>
            <th>文件版本</th>
            <th>内部版本</th>
            <th>编译时间</th>
            <th>描述</th>
          </tr>
        </thead>
        <tbody>
          <!-- @foreach (AssemblyX item in ViewBag.myAsms)
            {
                <tr>
                    <td>@item.name</td>
                    <td>@item.title</td>
                    <td>@item.fileVersion</td>
                    <td>@item.version</td>
                    <td>@(item.compile.year <= 2000 ? "" : item.compile.toFullString())</td>
                    <td>@item.description</td>
                </tr>
            } -->
        </tbody>
      </table>
    </div>
  </div>
</template>

<script>
export default {
  data() {
    return {
      model: {
        rawUrl: 'RawUrl',
        contentRootPath: 'ContentRootPath',
        host: 'Host',
        localHost: 'LocalHost',
        remoteHost: 'RemoteHost',
        commandLine: 'CommandLine',
        processName: 'ProcessName',
        curDomainFriendlyName: 'FriendlyName',
        envVersion: 'EnvVersion',
        frameworkName: 'FrameworkName',
        guid: 'Guid',
        oSName: 'OSName',
        oSVersion: 'OSVersion',
        product: 'Product',
        userName: 'UserName',
        machineName: 'MachineName',
        uuid: 'UUID',
        processor: 'Processor',
        processorCount: 'ProcessorCount',
        cpuRate: 'CpuRate',
        temperature: 0,
        dateTimeNow: 'DateTimeNow',
        uptime: 'Uptime',
        availableMemory: 'AvailableMemory',
        memory: 'Memory',
        workingSet64: 'WorkingSet64',
        privateMemorySize64: 'PrivateMemorySize64',
        totalMemory: 'TotalMemory',
      },
    }
  },
  methods: {
    restart() {
      this.$alert(
        '仅重启ASP.Net Core应用程序域，而不是操作系统！<br/>确认重启？',
        '提示',
        {
          confirmButtonText: 'confirm',
          callback: (action) => {
            console.log(action)
          },
        }
      )
    },
    memoryFree() {
      console.log('memoryFree')
    },
    main(id) {
      console.log(id)
    },
  },
}
</script>

<style></style>
