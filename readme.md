# Loans API – Documentation

Loans API არის RESTful სერვისი, რომელიც უზრუნველყოფს სესხების შექმნას, განახლებას, წაშლას და სტატუსების მართვას. API იყენებს JWT ავტორიზაციას და Role-based დაშვების კონტროლს (User, Accountant).

## Overview

სისტემა იძლევა საშუალებას:

- მომხმარებელმა შექმნას საკუთარი სესხი
- განაახლოს და წაშალოს მხოლოდ საკუთარი სესხი
- იხილოს საკუთარი სესხების სია
- მოდერატორს შეუძლია ნებისმიერი იუზერის სესხების ნახვა, განახლება და წაშლა
- მოდერატორს შეუძლია იუზერის განბლოკვა/დაბლოკვა


## Technologies

- ASP.NET Core
- Entity Framework Core
- JWT Authentication
- NLog
- SQLite
- FluentValidation

### UserController

| Method | Route | Description | Roles |
|--------|-------|-------------|-------|
| POST | `/api/user/register` | რეგისტრაცია | Guest |
| POST | `/api/user/login` | შესვლა (JWT Token) | Guest |
| GET | `/api/user/{id}` | მომხმარებლის ინფორმაციის მიღება | User |

### LoanController

| Method | Route | Description | Roles |
|--------|-------|-------------|-------|
| POST | `/api/loan/create` | ახალი სესხის შექმნა | User |
| PATCH | `/api/loan/updateStatus` | სესხის სტატუსის განახლება | Accountant |
| GET | `/api/loan/my-loans` | საკუთარი სესხების ნახვა | User |
| PUT | `/api/loan/update` | საკუთარი სესხის განახლება | User |
| DELETE | `/api/loan/delete` | საკუთარი სესხის წაშლა | User |
| GET | `/api/loan/all-loans?userId={id}` | ნებისმიერ მომხმარებლის სესხების ნახვა | Accountant |
| PUT | `/api/loan/update-any` | ნებისმიერ მომხმარებლის სესხის განახლება | Accountant |
| DELETE | `/api/loan/delete-any?loanId={id}` | ნებისმიერ მომხმარებლის სესხის წაშლა | Accountant |

### AccountantController

| Method | Route | Description | Roles |
|--------|-------|-------------|-------|
| PUT | `/api/accountant/block-user?id={id}` | მომხმარებლის დაბლოკვა | Accountant |
| PUT | `/api/accountant/unblock-user?id={id}` | მომხმარებლის განბლოკვა | Accountant |


