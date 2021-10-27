export function loadEditor(editComponent, options) {
  autosize(document.querySelectorAll('.autosize'));
  window.onscroll = function () { makeToolbarSticky(toolbar) };

  let toolbarItems = getToolbarItems(options);

  let editor = getEditor(editComponent, toolbarItems);

  addTooltipAttributes();

  return editor;
}

function getToolbarItems(options) {
  if(!options || !options.toolbarItems) return;

  const toolbarItems = options.toolbarItems.map(item => {
    if(item.name === '|') return '|';

    var tbItem = {
      name: item.name,
      action: getToolbarItemAction(item),
      icon: item.icon,
      title: item.title,
      noDisable: !item.canDisable
    };

    if(item.className !== '') {
      tbItem.className = item.className;
    }

    return tbItem;
  });

  return toolbarItems;
}

function addTooltipAttributes() {
  let buttons = document.querySelectorAll('.editor-toolbar button');
  for (let i = 0; i < buttons.length; i++) {
    buttons[i].setAttribute('data-bs-toggle', 'tooltip');
    buttons[i].setAttribute('data-bs-placement', 'bottom');
  }
}

function makeToolbarSticky(toolbar) {
  if (toolbar != "miniToolbar") {
    let body = document.querySelector("body");
    let editorWrapper = document.querySelector(".easymde-wrapper");
    let sticky = editorWrapper.offsetTop;
    if (window.pageYOffset > sticky) {
      body.classList.add("toolbar-sticky");
    } else {
      body.classList.remove("toolbar-sticky");
    }
  }
}

function getToolbarItemAction(toolbarItem) {
  switch(toolbarItem.name) {
    case "heading": return EasyMDE.toggleHeadingSmaller;
    case "bold": return EasyMDE.toggleBold;
    case "italic": return EasyMDE.toggleItalic;
    case "strikethrough": return EasyMDE.toggleStrikethrough;
    case "unordered-list": return EasyMDE.toggleUnorderedList;
    case "ordered-list": return EasyMDE.toggleOrderedList;
    case "quote": return EasyMDE.toggleBlockquote;
    case "link": return EasyMDE.drawLink;
    case "table": return EasyMDE.drawTable;
    case "code": return EasyMDE.toggleCodeBlock;
    case "horizontal-rule": return EasyMDE.drawHorizontalRule;
    case "clean-block": return EasyMDE.cleanBlock;
    case "preview": return EasyMDE.togglePreview;
    case "side-by-side": return EasyMDE.toggleSideBySide;
    case "fullscreen": return EasyMDE.toggleFullScreen;
    default: return (editor) => toolbarItem.callback();
  }
}

function getEditor(editComponent, toolbarItems) {
  let easyMDE = new EasyMDE({
    element: editComponent,
    autoDownloadFontAwesome: false,
    indentWithTabs: false,
    status: false,
    height: "200px",
    minHeight: "200px",
    parsingConfig: {
      allowAtxHeaderWithoutSpace: true,
      underscoresBreakWords: true
    },
    renderingConfig: {
      singleLineBreaks: false,
      codeSyntaxHighlighting: true
    },
    toolbar: toolbarItems,
    insertTexts: {
      horizontalRule: ["", "\n---\n"]
    }
  });
  return easyMDE;
}

export function insertYoutube(easymde) {
  let id = prompt("Please enter video ID", "");

  if (id !== null && id !== "") {
    let tag = `<iframe width="700" height="400" src="https://www.youtube.com/embed/${id}" frameborder="0" allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture" allowfullscreen></iframe>`;
    replaceSelection(easymde, tag);
  }
}

export function replaceSelection(easymde, value) {
  easymde.codemirror.replaceSelection(value);
}

export function onPaste(easymde, callback) {
  easymde.codemirror.on("paste", function (self, event) {
    callback();
  });
}

export function onValueChanged(easymde, callback) {
  easymde.codemirror.on("change", function (self, event) {
    callback(easymde.value());
  });
}

export function getEditorValue(easymde) {
  return easymde.value();
}

export function setEditorValue(easymde, txt) {
  easymde.value(txt
    .replace(/&#xA;/g, '\r\n')
    .replace(/&#xD;/g, '')
    .replace(/&lt;/g, '<')
    .replace(/&gt;/g, '>')
    .replace(/&quot;/g, '"'));
}

DotNet.attachReviver(function (key, value) {
  if (value && typeof value === 'object' && value.hasOwnProperty("__isJsCallback")) {
      return function () {
        value.reference.invokeMethodAsync('Invoke', ...arguments);
      };
  } else {
      return value;
  }
});
