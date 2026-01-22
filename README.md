<p align="center"><picture>
  <source media="(prefers-color-scheme: dark)" width="492px" srcset="Logo/purpleblog_d.png">
  <source media="(prefers-color-scheme: light)" width="492px" srcset="Logo/purpleblog_l.png">
  <img alt="Logo" src="Logos/purpleblog_l.png">
</picture></p>

A tool for converting Markdown files to HTML pages with template.

Usage:

```
purpleblog -i=/input/path -o=/output/path -n="Blog name" -d="Blog description"
```

### Required arguments:

__-i__: Path to the folder containing folders with the index.md file.

__-o__: Path to the folder where HTML pages will be saved.

__-n__: Value of this argument will replace the `{{blogname}}` tag in the template. The value should be the blog name.

__-d__: Value of this argument will replace the `{{blogdesc}}` tag in the template. The value should be the blog description.

### Optional arguments:

__-it__: Path to the file with the HTML template for the home page (that contains links to posts). The template must be contains these tags: `{{blogname}}`, `{{blogdesc}}` and `{{content}}`

__-pt__: Path to the file with the HTML template for the posts page. The template must be contains these tags: `{{blogname}}`, `{{title}}`, `{{summary}}`, `{{published}}` and `{{content}}`

## File and Folder Structure

Inside the folder whose path you pass as the __-i__ argument (the example below uses `source`), there must be a separate folder for each post. The folder name is used as the URL slug (i.e., the relative URL to the post). Each of these folders must contain an `index.md` file, which represents the post itself.

You may also place images and other files related to the post in the same folder; however, PurpleBlog will not copy these files to the output folder. If you store everything on GitHub, you can instead reference such assets using their full paths, for example: `https://raw.githubusercontent.com/YourProfile/YourBlog/refs/heads/main/your-post/picture.jpg`.

```
source/
├── your-post/
│   ├── index.md
│   └── picture.jpg
└── how-to-make-a-thing/
    └── index.md
```

After PurpleBlog converts the files to HTML, it saves them in the output folder (referred to below as the “root” folder), whose path you pass as the __-o__ argument (the example below uses `destination`):

```
destination/             # root folder
├── index.html           # home page
├── posts.json           # JSON with posts info
├── your-post/
│   └── index.html
└── how-to-make-a-thing/
    └── index.html

```

The `index.html` file in the root directory contains links to all posts and effectively serves as the blog’s home page. The `posts.json` file contains descriptions of posts (metadata) in JSON format.

If you do not use custom templates (i.e., you did not pass the __-it__ and __-pt__ arguments), you can define your own blog styling. To do this, create a `style.css` file in the root folder.

## Metadata

Each `index.md` file must start with post metadata in the following format:

```
---
title: How to make a thing
summary: A guide on how to make a thing.
published: 2026-01-20
---
```

| Parameter | Description                                                                                                                   |
|-----------|-------------------------------------------------------------------------------------------------------------------------------|
| title     | The post title. In the default template, it is displayed as the post heading.                                                 |
| summary   | A short description of the post. In the default template, it is used in the `<meta name="description">` tag on the post page. |
| published | The publication date. In the default template, it is displayed below the title on the post page.                              |
| hidden    | _optional_ Don't add link to post in home page and `posts.json`                                                               |

__Important:__ The date specified in published must be in the __YYYY-MM-DD__ format.

On the home page, the `title`, `summary`, and `published` tags are used within this template for each post:

```
<p>
  <a href="{{relativeUrl}}">{{title}}</a>
  <span>{{published}}</span>
  <div class="summary">{{summary}}</div>
</p>
```

Here, `relativeUrl` is a URL to the post. The date in the `publised` tag is specified in the M/D format.

```
<p>
  <a href="how-to-make-a-thing/">How to make a thing</a>
  <span>1/20</span>
  <div class="summary">A guide on how to make a thing</div>
</p>
```