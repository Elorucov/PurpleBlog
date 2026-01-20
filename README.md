# PurpleBlog

A tool for converting Markdown files to HTML pages with template.

Usage:

```
purpleblog -i=/input/path -o=/output/path -n="Blog name" -d="Blog description"
```

#### Required arguments:

__-i__: Path to the folder containing folders with the index.md file.

__-o__: Path to the folder where HTML pages will be saved.

__-n__: Value of this argument will replace the `{{blogname}}` tag in the template. The value should be the blog name.

__-d__: Value of this argument will replace the `{{blogdesc}}` tag in the template. The value should be the blog description.

#### Optional arguments:

__-it__: Path to the file with the HTML template for the main index.html page (that contains links to posts). The template must be contains these tags: `{{blogname}}`, `{{blogdesc}}` and `{{content}}`

__-pt__: Path to the file with the HTML template for the posts page. The template must be contains these tags: `{{blogname}}`, `{{title}}`, {{summary}}, `{{published}}` and `{{content}}`