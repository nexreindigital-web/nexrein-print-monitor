@extends('errors.layout')

@section('title', '403 — Access Denied')
@section('code', '403')
@section('icon', '🛡️')
@section('icon_style', 'background: rgba(245, 158, 11, 0.15); border-color: rgba(245, 158, 11, 0.3);')
@section('heading', 'Access Forbidden')
@section('message', 'You do not have administrative privileges to view or perform operations on this workstation fleet resource.')
