# DebugCanvasWpf.DotNetFramework

[![nuget](https://img.shields.io/nuget/v/DebugCanvasWpf.DotNetFramework.svg)](https://www.nuget.org/packages/DebugCanvasWpf.DotNetFramework/)

A fast drawing canvas for debugging visualizations using GDI+ as a WPF component

## Features

- Simple Visualization elements: lines, boxes, x, text
- Fast Drawing of many elements (easily draw thousands of lines)
- Infinite Zoom and Mouse Dragging

## Sample Usage

- Add to xaml:

```
		...
        xmlns:comp="clr-namespace:DebugCanvasWpf.DotNetFramework;assembly=DebugCanvasWpf.DotNetFramework"
		...>
    <Grid>
        <comp:DebugCanvasComponent x:Name="compDebugCanvas"/>
    </Grid>
	...
```

- Add drawing elements and call `Render`

```
var d = compDebugCanvas.DrawingData;
d.DrawBox(new Vector2(10, 10), System.Drawing.Color.Red, new Vector2(100, 100), false);
d.DrawLine(new Vector2(10, 10), new Vector2(1000, 1000), System.Drawing.Color.Blue);
d.DrawText("Hello World!", new Vector2(20, 20), System.Drawing.Color.Yellow, new Vector2(200, 20), 16, shadow: System.Drawing.Color.Blue);

compDebugCanvas.Render();
```

## License

MIT